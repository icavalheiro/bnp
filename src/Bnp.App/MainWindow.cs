using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;
using AvaloniaRichEditor.Controls;
using Bnp.Core.Documents;
using Bnp.Diagnostics;
using Bnp.Localization;
using Bnp.Presentation;
using Bnp.Services;
using Bnp.Services.CloudBackup;
using Lucide.Avalonia;

namespace Bnp;

public sealed class MainWindow : Window, IDisposable
{
    private const double EditorFontSizePoints = 11.25;

    private readonly IDocumentRepository _repository;
    private readonly AutosaveCoordinator _autosave;
    private readonly CloudBackupService _cloudBackupService;
    private readonly CloudBackupCoordinator _cloudBackupCoordinator;
    private readonly List<DocumentSummary> _documents;
    private readonly ListBox _documentList = new();
    private readonly RichEditor _editor = new();
    private readonly TextBlock _saveStatus = new();
    private readonly Border _sidebar = new();
    private readonly ColumnDefinition _sidebarColumn = new();
    private readonly Border _windowFrame = new();
    private readonly Grid _rootLayout = new();
    private readonly ScrollViewer _editorSurface = new();
    private readonly Border _statusBorder = new();
    private readonly TextBlock _sidebarTitle = new();
    private readonly Button _addDocumentButton = new();
    private readonly DocumentSettingsFlyoutFactory _documentSettingsFlyoutFactory;
    private readonly EditorFormattingToolbar _editorToolbar;
    private readonly MainWindowChrome _chrome;
    private EditorCopy _copy;
    private DocumentRecord _currentDocument;
    private BnpPalette _palette = BnpTheme.GetPalette(ThemeVariant.Light);
    private bool _isLayoutReady;
    private bool _isLoadingDocument;
    private bool _isSidebarCollapsed;
    private string _themeKey;
    private string _languageKey;

    internal MainWindow(
        IDocumentRepository repository,
        WorkspaceSnapshot workspace,
        CloudBackupService cloudBackupService,
        CloudBackupCoordinator cloudBackupCoordinator)
    {
        _repository = repository;
        _cloudBackupService = cloudBackupService;
        _cloudBackupCoordinator = cloudBackupCoordinator;
        _documents = workspace.Documents.ToList();
        _currentDocument = workspace.ActiveDocument;
        _isSidebarCollapsed = workspace.IsSidebarCollapsed;
        _themeKey = workspace.ThemeKey;
        _languageKey = workspace.LanguageKey;
        _copy = EditorCopyCatalog.Load(_languageKey);
        _autosave = new AutosaveCoordinator(repository, TimeSpan.FromMilliseconds(350));
        _cloudBackupCoordinator.ConfigureMerge(_autosave.Flush, ReloadWorkspaceAfterCloudMerge);
        _documentSettingsFlyoutFactory = new DocumentSettingsFlyoutFactory(
            _copy,
            () => _palette,
            SaveDocumentSettings);

        Title = _copy.ApplicationTitle;
        Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://BNP/Assets/BNP.ico")));
        Width = 1100;
        Height = 720;
        MinWidth = 720;
        MinHeight = 480;
        WindowDecorations = WindowDecorations.None;
        CanResize = true;
        CanMinimize = true;
        CanMaximize = true;
        Background = Brushes.Transparent;
        TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent };
        BorderThickness = new Thickness(0);
        CornerRadius = new CornerRadius(8);
        ClipToBounds = true;
        Win32Properties.SetWindowCornerPreference(this, Win32Properties.WindowCornerPreference.RoundSmall);

        ConfigureEditor();
        _editorToolbar = new EditorFormattingToolbar(
            _editor,
            _copy,
            GetAutomaticTextBrush,
            () => _palette);
        _chrome = new MainWindowChrome(
            this,
            _windowFrame,
            () => _copy,
            () => _palette,
            ToggleSidebar,
            () => _themeKey,
            () => _languageKey,
            ApplyEditorPreferences,
            OpenCloudBackupSettings,
            _isSidebarCollapsed);
        Content = BuildLayout();
        _isLayoutReady = true;
        ApplyTheme();
        PopulateDocumentList();
        LoadDocument(_currentDocument);
        AttachEvents();
        Opened += OnWindowOpened;
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        Dispose();
        base.OnClosing(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (!_isLayoutReady)
        {
            return;
        }

        if (change.Property == ActualThemeVariantProperty)
        {
            ApplyTheme();
        }
        else if (change.Property == WindowStateProperty)
        {
            _chrome.ApplyWindowState();
        }
    }

    public void Dispose()
    {
        _autosave.Dispose();
    }

    private void OnWindowOpened(object? sender, EventArgs eventArgs)
    {
        Opened -= OnWindowOpened;
        ApplyTheme();
        Avalonia.Threading.Dispatcher.UIThread.Post(StartupMetrics.MarkReady, Avalonia.Threading.DispatcherPriority.Render);
    }

    private void ConfigureEditor()
    {
        _editor.AllowImages = false;
        _editor.AllowTables = false;
        _editor.AllowLocalFileImages = false;
        _editor.AllowRemoteImagesOnPaste = false;
        _editor.DefaultFontFamily = new FontFamily("avares://BNP/Assets/Fonts#Inconsolata");
        _editor.DefaultFontSize = EditorFontSizePoints;
        AutomationProperties.SetName(_editor, _copy.DocumentEditor);
    }

    private void OpenCloudBackupSettings()
    {
        var window = new CloudBackupSettingsWindow(
            _cloudBackupService,
            _cloudBackupCoordinator,
            _copy);
        _ = window.ShowDialog(this);
    }

    private void ReloadWorkspaceAfterCloudMerge()
    {
        var workspace = _repository.GetWorkspace();
        _documents.Clear();
        _documents.AddRange(workspace.Documents);
        _currentDocument = workspace.ActiveDocument;

        ApplyEditorPreferenceValues(workspace.ThemeKey, workspace.LanguageKey);
        _isSidebarCollapsed = workspace.IsSidebarCollapsed;
        _sidebar.IsVisible = !_isSidebarCollapsed;
        _sidebarColumn.Width = _isSidebarCollapsed ? new GridLength(0) : new GridLength(252);
        _chrome.SetSidebarCollapsed(_isSidebarCollapsed);
        PopulateDocumentList();
        LoadDocument(workspace.ActiveDocument);
    }

    private Border BuildLayout()
    {
        _sidebarColumn.Width = _isSidebarCollapsed ? new GridLength(0) : new GridLength(252);

        _rootLayout.ColumnDefinitions = new ColumnDefinitions { _sidebarColumn, new(GridLength.Star) };
        _rootLayout.RowDefinitions = new RowDefinitions("Auto,*,Auto");

        var header = _chrome.Header;
        Grid.SetColumnSpan(header, 2);

        _sidebar.Child = BuildSidebar();
        _sidebar.IsVisible = !_isSidebarCollapsed;
        _sidebar.BorderThickness = new Thickness(0, 0, 1, 0);
        _sidebar.BorderBrush = Brushes.Gray;
        Grid.SetRow(_sidebar, 1);

        var editorArea = BuildEditorArea();
        Grid.SetColumn(editorArea, 1);
        Grid.SetRow(editorArea, 1);

        var status = BuildStatusBar();
        Grid.SetColumnSpan(status, 2);
        Grid.SetRow(status, 2);

        _rootLayout.Children.Add(header);
        _rootLayout.Children.Add(_sidebar);
        _rootLayout.Children.Add(editorArea);
        _rootLayout.Children.Add(status);
        _chrome.AddResizeZones(_rootLayout);

        _windowFrame.Child = _rootLayout;
        _windowFrame.CornerRadius = new CornerRadius(8);
        _windowFrame.ClipToBounds = true;
        _windowFrame.BorderThickness = new Thickness(1);
        return _windowFrame;
    }

    private bool ToggleSidebar()
    {
        _isSidebarCollapsed = !_isSidebarCollapsed;
        _sidebar.IsVisible = !_isSidebarCollapsed;
        _sidebarColumn.Width = _isSidebarCollapsed ? new GridLength(0) : new GridLength(252);
        _repository.SetSidebarCollapsed(_isSidebarCollapsed);
        return _isSidebarCollapsed;
    }

    private Grid BuildSidebar()
    {
        ConfigureIconButton(_addDocumentButton, LucideIconKind.Plus, _copy.NewDocument);
        _addDocumentButton.Click += (_, _) => CreateDocument();

        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            Children = { _addDocumentButton }
        };
        _sidebarTitle.Text = _copy.Documents;
        _sidebarTitle.FontSize = 12;
        _sidebarTitle.FontWeight = FontWeight.SemiBold;
        _sidebarTitle.VerticalAlignment = VerticalAlignment.Center;

        var sidebarHeader = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            Margin = new Thickness(10, 8, 8, 6),
            Children = { _sidebarTitle, actions }
        };
        Grid.SetColumn(actions, 1);

        _documentList.HorizontalAlignment = HorizontalAlignment.Stretch;
        _documentList.Background = Brushes.Transparent;
        _documentList.BorderThickness = new Thickness(0);
        _documentList.Padding = new Thickness(0, 3, 0, 8);
        AutomationProperties.SetName(_documentList, _copy.OpenDocuments);

        var sidebarLayout = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*"),
            Children = { sidebarHeader, _documentList }
        };
        Grid.SetRow(_documentList, 1);
        return sidebarLayout;
    }

    private Border BuildStatusBar()
    {
        _saveStatus.Text = _copy.Saved;
        _saveStatus.HorizontalAlignment = HorizontalAlignment.Right;

        _saveStatus.FontSize = 12;
        _statusBorder.MinHeight = 26;
        _statusBorder.Padding = new Thickness(12, 4);
        _statusBorder.BorderThickness = new Thickness(0, 1, 0, 0);
        _statusBorder.Child = _saveStatus;
        return _statusBorder;
    }

    private Grid BuildEditorArea()
    {
        _editorSurface.Content = _editor;
        _editorSurface.Padding = new Thickness(12, 8);
        _editorSurface.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        _editorSurface.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;

        var editorArea = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*"),
            Children = { _editorToolbar.View, _editorSurface }
        };
        Grid.SetRow(_editorSurface, 1);
        return editorArea;
    }

    private void PopulateDocumentList()
    {
        _documentList.Items.Clear();
        ListBoxItem? activeItem = null;

        foreach (var document in _documents.OrderBy(document => document.TabOrder))
        {
            var item = CreateDocumentListItem(document);
            _documentList.Items.Add(item);
            if (document.Id == _currentDocument.Id)
            {
                activeItem = item;
            }
        }

        _documentList.SelectedItem = activeItem;
    }

    private ListBoxItem CreateDocumentListItem(DocumentSummary document)
    {
        var documentIcon = BnpIcons.CreateDocumentIcon(document.IconKey, document.ColorKey);
        var title = new TextBlock
        {
            Text = document.Title,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        var settingsButton = CreateIconButton(
            LucideIconKind.Settings,
            string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                _copy.ConfigureDocument,
                document.Title));
        settingsButton.Width = 28;
        settingsButton.Height = 28;
        settingsButton.Padding = new Thickness(5);
        settingsButton.IsVisible = false;

        var content = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
            ColumnSpacing = 9,
            Children = { documentIcon, title, settingsButton }
        };
        Grid.SetColumn(title, 1);
        Grid.SetColumn(settingsButton, 2);

        var item = new ListBoxItem
        {
            Tag = document.Id,
            MinHeight = 38,
            Margin = new Thickness(6, 2),
            Padding = new Thickness(8, 6),
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Content = content
        };
        var settingsFlyout = _documentSettingsFlyoutFactory.Create(document);
        FlyoutBase.SetAttachedFlyout(settingsButton, settingsFlyout);
        settingsButton.Click += (_, eventArgs) =>
        {
            _documentList.SelectedItem = item;
            FlyoutBase.ShowAttachedFlyout(settingsButton);
            eventArgs.Handled = true;
        };
        item.PointerEntered += (_, _) => settingsButton.IsVisible = true;
        item.PointerExited += (_, _) => settingsButton.IsVisible = false;
        return item;
    }

    private bool SaveDocumentSettings(
        Guid documentId,
        string? titleValue,
        string iconKey,
        string colorKey)
    {
        var title = titleValue?.Trim();
        if (string.IsNullOrEmpty(title))
        {
            return false;
        }

        _autosave.Flush();
        var document = documentId == _currentDocument.Id
            ? _currentDocument
            : _repository.GetDocument(documentId);
        if (document is null)
        {
            _saveStatus.Text = _copy.DocumentUnavailable;
            return false;
        }

        var updatedDocument = document with
        {
            Title = title,
            IconKey = iconKey,
            ColorKey = colorKey,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        _repository.SaveDocument(updatedDocument);
        if (documentId == _currentDocument.Id)
        {
            _currentDocument = updatedDocument;
            _chrome.SetDocumentTitle(title);
            Title = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                _copy.WindowTitle,
                title);
        }

        var index = _documents.FindIndex(summary => summary.Id == documentId);
        if (index >= 0)
        {
            _documents[index] = ToSummary(updatedDocument);
        }

        PopulateDocumentList();
        _saveStatus.Text = _copy.Saved;
        return true;
    }

    private void AttachEvents()
    {
        _editor.TextChanged += (_, _) =>
        {
            ApplyDocumentTextTheme();
            if (!_isLoadingDocument)
            {
                _autosave.Queue(CreateCurrentSnapshot);
            }
        };
        _autosave.StatusChanged += status =>
        {
            _saveStatus.Text = status switch
            {
                SaveStatus.Unsaved => _copy.Unsaved,
                SaveStatus.Saving => _copy.Saving,
                SaveStatus.Failed => _copy.SaveFailed,
                _ => _copy.Saved
            };
        };
        _documentList.SelectionChanged += (_, _) => SwitchToSelectedDocument();
        AddHandler(
            InputElement.KeyDownEvent,
            OnWindowKeyDown,
            RoutingStrategies.Tunnel,
            handledEventsToo: true);
    }

    private void CreateDocument()
    {
        _autosave.Flush();

        var document = _repository.CreateDocument(
            string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                _copy.UntitledDocument,
                _documents.Count + 1));
        _documents.Add(ToSummary(document));
        var item = CreateDocumentListItem(ToSummary(document));
        _documentList.Items.Add(item);
        _documentList.SelectedItem = item;
    }

    private void SwitchToSelectedDocument()
    {
        if (_documentList.SelectedItem is not ListBoxItem { Tag: Guid documentId } ||
            documentId == _currentDocument.Id)
        {
            return;
        }

        _autosave.Flush();
        var document = _repository.GetDocument(documentId);
        if (document is null)
        {
            _saveStatus.Text = _copy.DocumentUnavailable;
            return;
        }

        _repository.SetActiveDocument(documentId);
        LoadDocument(document);
    }

    private void LoadDocument(DocumentRecord document)
    {
        _isLoadingDocument = true;
        try
        {
            _currentDocument = document;
            _chrome.SetDocumentTitle(document.Title);
            Title = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                _copy.WindowTitle,
                document.Title);

            if (document.ContentFormat == DocumentFormats.AvaloniaRichEditorJsonV1)
            {
                _editor.LoadJson(document.Content);
            }
            else
            {
                _editor.Clear();
                _editor.InsertText(document.Content);
            }

            ApplyDocumentTextTheme();
            _editorToolbar.SyncFromCaret();
            _editor.MarkSaved();
            _saveStatus.Text = _copy.Saved;
        }
        finally
        {
            _isLoadingDocument = false;
        }
    }

    private DocumentRecord CreateCurrentSnapshot()
    {
        _currentDocument = _currentDocument with
        {
            ContentFormat = DocumentFormats.AvaloniaRichEditorJsonV1,
            Content = _editor.ToJson(),
            UpdatedAt = DateTimeOffset.UtcNow
        };
        return _currentDocument;
    }

    private void OnWindowKeyDown(object? sender, KeyEventArgs eventArgs)
    {
        if (!eventArgs.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            return;
        }

        if (eventArgs.Key == Key.B && eventArgs.KeyModifiers == KeyModifiers.Control)
        {
            _chrome.ToggleSidebar();
            eventArgs.Handled = true;
        }
        else if (eventArgs.Key == Key.N)
        {
            CreateDocument();
            eventArgs.Handled = true;
        }
        else if (eventArgs.Key == Key.S)
        {
            _autosave.Flush();
            eventArgs.Handled = true;
        }
    }

    private Button CreateIconButton(LucideIconKind icon, string accessibleName)
    {
        var button = new Button
        {
        };
        ConfigureIconButton(button, icon, accessibleName);
        return button;
    }

    private void ConfigureIconButton(Button button, LucideIconKind icon, string accessibleName)
    {
        button.Content = BnpIcons.Create(icon);
        button.Width = 32;
        button.Height = 32;
        button.Padding = new Thickness(6);
        button.Background = Brushes.Transparent;
        button.BorderBrush = Brushes.Transparent;
        button.CornerRadius = new CornerRadius(5);
        button.HorizontalContentAlignment = HorizontalAlignment.Center;
        button.VerticalContentAlignment = VerticalAlignment.Center;
        button.PointerEntered += (_, _) => button.Background = _palette.ButtonHover;
        button.PointerExited += (_, _) => button.Background = Brushes.Transparent;
        ToolTip.SetTip(button, accessibleName);
        AutomationProperties.SetName(button, accessibleName);
    }

    private void ApplyEditorPreferences(string themeKey, string languageKey)
    {
        _repository.SetEditorPreferences(themeKey, languageKey);
        ApplyEditorPreferenceValues(themeKey, languageKey);
    }

    private void ApplyEditorPreferenceValues(string themeKey, string languageKey)
    {
        _themeKey = themeKey;
        RequestedThemeVariant = EditorThemePreference.ToThemeVariant(themeKey);

        if (_languageKey == languageKey)
        {
            return;
        }

        _languageKey = languageKey;
        System.Globalization.CultureInfo.CurrentUICulture =
            System.Globalization.CultureInfo.GetCultureInfo(languageKey);
        _copy = EditorCopyCatalog.Load(languageKey);
        _documentSettingsFlyoutFactory.ApplyCopy(_copy);
        _editorToolbar.ApplyCopy(_copy);
        _chrome.ApplyCopy();

        AutomationProperties.SetName(_editor, _copy.DocumentEditor);
        ToolTip.SetTip(_addDocumentButton, _copy.NewDocument);
        AutomationProperties.SetName(_addDocumentButton, _copy.NewDocument);
        _sidebarTitle.Text = _copy.Documents;
        AutomationProperties.SetName(_documentList, _copy.OpenDocuments);
        _saveStatus.Text = _copy.Saved;
        Title = string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            _copy.WindowTitle,
            _currentDocument.Title);
        PopulateDocumentList();
    }

    private void ApplyTheme()
    {
        _palette = BnpTheme.GetPalette(ActualThemeVariant);

        Foreground = _palette.PrimaryText;
        _windowFrame.Background = _palette.Window;
        _windowFrame.BorderBrush = _palette.Border;
        _rootLayout.Background = _palette.Window;
        _chrome.ApplyTheme();
        _sidebar.Background = _palette.Sidebar;
        _sidebar.BorderBrush = _palette.Border;
        _editorToolbar.ApplyPalette();
        _editorSurface.Background = _palette.Editor;
        _statusBorder.Background = _palette.Status;
        _statusBorder.BorderBrush = _palette.Border;
        _documentList.Foreground = _palette.PrimaryText;
        _saveStatus.Foreground = _palette.SecondaryText;
        _editor.CaretBrush = _palette.PrimaryText;
        _editor.SelectionBrush = _palette.Selection;
        ApplyDocumentTextTheme();
    }

    private void ApplyDocumentTextTheme()
    {
        RichEditorThemeApplicator.Apply(_editor, GetAutomaticTextBrush());
    }

    private IBrush GetAutomaticTextBrush()
    {
        return ActualThemeVariant == ThemeVariant.Dark
            ? Brushes.White
            : _palette.PrimaryText;
    }

    private static DocumentSummary ToSummary(DocumentRecord document)
    {
        return new DocumentSummary(
            document.Id,
            document.Title,
            document.IconKey,
            document.ColorKey,
            document.TabOrder,
            document.UpdatedAt);
    }
}