using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using AvaloniaRichEditor.Controls;
using AvaloniaRichEditor.Documents;
using Bnp.Core.Documents;
using Bnp.Diagnostics;
using Bnp.Presentation;
using Bnp.Services;
using Lucide.Avalonia;

namespace Bnp;

public sealed class MainWindow : Window, IDisposable
{
    private static readonly (string Label, string Color)[] TextColorOptions =
    [
        ("Red", "#E53935"),
        ("Orange", "#FB8C00"),
        ("Yellow", "#FDD835"),
        ("Green", "#43A047"),
        ("Teal", "#00897B"),
        ("Blue", "#1E88E5"),
        ("Purple", "#8E24AA"),
        ("Gray", "#616161")
    ];

    private readonly IDocumentRepository _repository;
    private readonly AutosaveCoordinator _autosave;
    private readonly List<DocumentSummary> _documents;
    private readonly ListBox _documentList = new();
    private readonly RichEditor _editor = new();
    private readonly TextBlock _titleDisplay = new();
    private readonly TextBlock _saveStatus = new();
    private readonly Border _sidebar = new();
    private readonly ColumnDefinition _sidebarColumn = new();
    private readonly Border _windowFrame = new();
    private readonly Grid _rootLayout = new();
    private readonly Border _headerBorder = new();
    private readonly Border _toolbarBorder = new();
    private readonly ScrollViewer _editorSurface = new();
    private readonly Border _statusBorder = new();
    private readonly TextBlock _sidebarTitle = new();
    private readonly TextBlock _brandText = new();
    private readonly Button _minimizeButton = new();
    private readonly Button _maximizeButton = new();
    private readonly Button _closeButton = new();
    private readonly List<Control> _resizeZones = new();
    private Button? _textColorButton;
    private IBrush? _activeTextColor;
    private DocumentRecord _currentDocument;
    private BnpPalette _palette = BnpTheme.GetPalette(ThemeVariant.Light);
    private bool _isLayoutReady;
    private bool _isLoadingDocument;
    private bool _isSidebarCollapsed;

    public MainWindow(IDocumentRepository repository, WorkspaceSnapshot workspace)
    {
        _repository = repository;
        _documents = workspace.Documents.ToList();
        _currentDocument = workspace.ActiveDocument;
        _isSidebarCollapsed = workspace.IsSidebarCollapsed;
        _autosave = new AutosaveCoordinator(repository, TimeSpan.FromMilliseconds(350));

        Title = "BNP";
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
            ApplyWindowState();
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
        _editor.DefaultFontSize = 12;
        AutomationProperties.SetName(_editor, "Document editor");
    }

    private Border BuildLayout()
    {
        _sidebarColumn.Width = _isSidebarCollapsed ? new GridLength(0) : new GridLength(252);

        _rootLayout.ColumnDefinitions = new ColumnDefinitions { _sidebarColumn, new(GridLength.Star) };
        _rootLayout.RowDefinitions = new RowDefinitions("Auto,*,Auto");

        var header = BuildHeader();
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
        AddResizeZones();

        _windowFrame.Child = _rootLayout;
        _windowFrame.CornerRadius = new CornerRadius(8);
        _windowFrame.ClipToBounds = true;
        _windowFrame.BorderThickness = new Thickness(1);
        return _windowFrame;
    }

    private Border BuildHeader()
    {
        var collapseButton = CreateIconButton(
            _isSidebarCollapsed ? LucideIconKind.PanelLeftOpen : LucideIconKind.PanelLeftClose,
            "Toggle document sidebar");
        collapseButton.Click += (_, _) =>
        {
            _isSidebarCollapsed = !_isSidebarCollapsed;
            _sidebar.IsVisible = !_isSidebarCollapsed;
            _sidebarColumn.Width = _isSidebarCollapsed ? new GridLength(0) : new GridLength(252);
            collapseButton.Content = BnpIcons.Create(
                _isSidebarCollapsed ? LucideIconKind.PanelLeftOpen : LucideIconKind.PanelLeftClose);
            _repository.SetSidebarCollapsed(_isSidebarCollapsed);
        };

        _titleDisplay.Width = 360;
        _titleDisplay.MinWidth = 180;
        _titleDisplay.MaxWidth = 480;
        _titleDisplay.HorizontalAlignment = HorizontalAlignment.Center;
        _titleDisplay.TextAlignment = TextAlignment.Center;
        _titleDisplay.TextTrimming = TextTrimming.CharacterEllipsis;
        _titleDisplay.FontSize = 14;
        _titleDisplay.FontWeight = FontWeight.SemiBold;
        _titleDisplay.VerticalAlignment = VerticalAlignment.Center;
        _titleDisplay.IsHitTestVisible = false;
        AutomationProperties.SetName(_titleDisplay, "Current document title");

        // _brandText.Text = "BNP";
        // _brandText.FontSize = 14;
        // _brandText.FontWeight = FontWeight.SemiBold;
        // _brandText.VerticalAlignment = VerticalAlignment.Center;

        var brand = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 9,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                collapseButton,
                new Image
                {
                    Source = new Bitmap(AssetLoader.Open(new Uri("avares://BNP/Assets/BNP.ico"))),
                    Width = 19,
                    Height = 19,
                    Stretch = Stretch.Uniform
                },
                // _brandText
            }
        };
        var captionButtons = BuildCaptionButtons();
        var headerLayout = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto,*"),
            Background = Brushes.Transparent,
            Children = { brand, _titleDisplay, captionButtons }
        };
        Grid.SetColumn(_titleDisplay, 1);
        Grid.SetColumn(captionButtons, 2);
        headerLayout.PointerPressed += OnTitleBarPointerPressed;

        _headerBorder.MinHeight = 44;
        _headerBorder.Padding = new Thickness(8, 0, 0, 0);
        _headerBorder.BorderThickness = new Thickness(0, 0, 0, 1);
        _headerBorder.Child = headerLayout;
        return _headerBorder;
    }

    private StackPanel BuildCaptionButtons()
    {
        ConfigureCaptionButton(_minimizeButton, LucideIconKind.Minus, "Minimize window");
        _minimizeButton.Click += (_, _) => WindowState = WindowState.Minimized;

        ConfigureCaptionButton(_maximizeButton, LucideIconKind.Maximize2, "Maximize window");
        _maximizeButton.Click += (_, _) => ToggleMaximize();

        ConfigureCaptionButton(_closeButton, LucideIconKind.X, "Close window");
        _closeButton.Click += (_, _) => Close();
        _closeButton.PointerEntered += (_, _) =>
        {
            _closeButton.Background = new SolidColorBrush(Color.Parse("#C42B1C"));
            _closeButton.Foreground = Brushes.White;
        };
        _closeButton.PointerExited += (_, _) =>
        {
            _closeButton.Background = Brushes.Transparent;
            _closeButton.Foreground = _palette.PrimaryText;
        };

        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Stretch,
            Children = { _minimizeButton, _maximizeButton, _closeButton }
        };
    }

    private void ConfigureCaptionButton(Button button, LucideIconKind icon, string accessibleName)
    {
        button.Content = BnpIcons.Create(icon, 16);
        button.Width = 46;
        button.MinHeight = 43;
        button.Padding = new Thickness(0);
        button.Focusable = false;
        button.Background = Brushes.Transparent;
        button.BorderBrush = Brushes.Transparent;
        button.BorderThickness = new Thickness(0);
        button.CornerRadius = new CornerRadius(0);
        button.HorizontalContentAlignment = HorizontalAlignment.Center;
        button.VerticalContentAlignment = VerticalAlignment.Center;
        button.PointerEntered += (_, _) => button.Background = _palette.ButtonHover;
        button.PointerExited += (_, _) => button.Background = Brushes.Transparent;
        ToolTip.SetTip(button, accessibleName);
        AutomationProperties.SetName(button, accessibleName);
    }

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs eventArgs)
    {
        if (!eventArgs.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (eventArgs.ClickCount == 2 && CanMaximize)
        {
            ToggleMaximize();
            eventArgs.Handled = true;
            return;
        }

        BeginMoveDrag(eventArgs);
        eventArgs.Handled = true;
    }

    private void ToggleMaximize()
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void AddResizeZones()
    {
        AddResizeZone(WindowEdge.North, horizontalAlignment: HorizontalAlignment.Stretch,
            verticalAlignment: VerticalAlignment.Top, height: 6);
        AddResizeZone(WindowEdge.South, horizontalAlignment: HorizontalAlignment.Stretch,
            verticalAlignment: VerticalAlignment.Bottom, height: 6);
        AddResizeZone(WindowEdge.West, horizontalAlignment: HorizontalAlignment.Left,
            verticalAlignment: VerticalAlignment.Stretch, width: 6);
        AddResizeZone(WindowEdge.East, horizontalAlignment: HorizontalAlignment.Right,
            verticalAlignment: VerticalAlignment.Stretch, width: 6);
        AddResizeZone(WindowEdge.NorthWest, HorizontalAlignment.Left, VerticalAlignment.Top, 10, 10);
        AddResizeZone(WindowEdge.NorthEast, HorizontalAlignment.Right, VerticalAlignment.Top, 10, 10);
        AddResizeZone(WindowEdge.SouthWest, HorizontalAlignment.Left, VerticalAlignment.Bottom, 10, 10);
        AddResizeZone(WindowEdge.SouthEast, HorizontalAlignment.Right, VerticalAlignment.Bottom, 10, 10);
    }

    private void AddResizeZone(
        WindowEdge edge,
        HorizontalAlignment horizontalAlignment,
        VerticalAlignment verticalAlignment,
        double width = double.NaN,
        double height = double.NaN)
    {
        var zone = new Border
        {
            Width = width,
            Height = height,
            Background = Brushes.Transparent,
            HorizontalAlignment = horizontalAlignment,
            VerticalAlignment = verticalAlignment,
            ZIndex = 1000
        };
        zone.PointerPressed += (_, eventArgs) =>
        {
            if (WindowState == WindowState.Normal &&
                eventArgs.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                BeginResizeDrag(edge, eventArgs);
                eventArgs.Handled = true;
            }
        };
        Grid.SetColumnSpan(zone, 2);
        Grid.SetRowSpan(zone, 3);
        _rootLayout.Children.Add(zone);
        _resizeZones.Add(zone);
    }

    private Grid BuildSidebar()
    {
        var addButton = CreateIconButton(LucideIconKind.Plus, "New document");
        addButton.Click += (_, _) => CreateDocument();

        var iconButton = CreateIconButton(LucideIconKind.Palette, "Choose document icon");
        AttachIconMenu(iconButton);

        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            Children = { iconButton, addButton }
        };
        _sidebarTitle.Text = "Documents";
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
        AutomationProperties.SetName(_documentList, "Open documents");

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
        _saveStatus.Text = "Saved";
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
        _textColorButton = CreateIconButton(LucideIconKind.Palette, "Text color");
        _textColorButton.Focusable = false;
        AttachTextColorMenu(_textColorButton);
        UpdateTextColorButton();

        var toolbar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 3,
            Margin = new Thickness(10, 5),
            Children =
            {
                CreateFormattingButton(LucideIconKind.Undo2, "Undo", _editor.Undo),
                CreateFormattingButton(LucideIconKind.Redo2, "Redo", _editor.Redo),
                CreateToolbarSeparator(),
                CreateFormattingButton(LucideIconKind.Bold, "Bold", _editor.ToggleBold),
                CreateFormattingButton(LucideIconKind.Italic, "Italic", _editor.ToggleItalic),
                CreateFormattingButton(
                    LucideIconKind.Highlighter,
                    "Highlight",
                    () => _editor.SetHighlight(_palette.Highlight)),
                _textColorButton,
                CreateToolbarSeparator(),
                CreateFormattingButton(
                    LucideIconKind.TextAlignStart,
                    "Align left",
                    () => _editor.SetTextAlignment(TextAlignment.Left)),
                CreateFormattingButton(
                    LucideIconKind.TextAlignCenter,
                    "Align center",
                    () => _editor.SetTextAlignment(TextAlignment.Center)),
                CreateFormattingButton(
                    LucideIconKind.TextAlignEnd,
                    "Align right",
                    () => _editor.SetTextAlignment(TextAlignment.Right))
            }
        };

        _toolbarBorder.MinHeight = 43;
        _toolbarBorder.BorderThickness = new Thickness(0, 0, 0, 1);
        _toolbarBorder.Child = toolbar;
        _editorSurface.Content = _editor;
        _editorSurface.Padding = new Thickness(32, 24);
        _editorSurface.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        _editorSurface.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;

        var editorArea = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*"),
            Children = { _toolbarBorder, _editorSurface }
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
        var titleEditor = new TextBox
        {
            Text = document.Title,
            Tag = document.Id,
            Padding = new Thickness(2, 0),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        AutomationProperties.SetName(titleEditor, $"Rename {document.Title}");

        var item = new ListBoxItem
        {
            Tag = document.Id,
            MinHeight = 38,
            Margin = new Thickness(6, 2),
            Padding = new Thickness(8, 6),
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 9,
                Children =
                {
                    BnpIcons.CreateDocumentIcon(document.IconKey),
                    titleEditor
                }
            }
        };
        titleEditor.GotFocus += (_, _) => _documentList.SelectedItem = item;
        titleEditor.LostFocus += (_, _) => RenameDocument(document.Id, titleEditor);
        titleEditor.KeyDown += (_, eventArgs) => OnTitleEditorKeyDown(document.Id, titleEditor, eventArgs);
        return item;
    }

    private void AttachEvents()
    {
        _editor.TextChanged += (_, _) =>
        {
            if (!_isLoadingDocument)
            {
                _autosave.Queue(CreateCurrentSnapshot);
            }
        };
        _editor.SelectionChanged += (_, _) => SyncTextColorFromCaret();
        _editor.AddHandler(
            InputElement.KeyDownEvent,
            OnEditorKeyDown,
            RoutingStrategies.Tunnel,
            handledEventsToo: true);
        _autosave.StatusChanged += status =>
        {
            _saveStatus.Text = status switch
            {
                SaveStatus.Unsaved => "Unsaved",
                SaveStatus.Saving => "Saving...",
                SaveStatus.Failed => "Save failed",
                _ => "Saved"
            };
        };
        _documentList.SelectionChanged += (_, _) => SwitchToSelectedDocument();
        KeyDown += OnWindowKeyDown;
    }

    private void CreateDocument()
    {
        _autosave.Flush();

        var document = _repository.CreateDocument($"Untitled {_documents.Count + 1}");
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
            _saveStatus.Text = "Document unavailable";
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
            _titleDisplay.Text = document.Title;
            Title = $"BNP - {document.Title}";

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
            SyncTextColorFromCaret();
            _editor.MarkSaved();
            _saveStatus.Text = "Saved";
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

    private void RenameDocument(Guid documentId, TextBox titleEditor)
    {
        var index = _documents.FindIndex(document => document.Id == documentId);
        if (index < 0)
        {
            return;
        }

        var currentTitle = _documents[index].Title;
        var title = titleEditor.Text?.Trim();
        if (string.IsNullOrEmpty(title))
        {
            titleEditor.Text = currentTitle;
            return;
        }

        if (title == currentTitle)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        _documents[index] = _documents[index] with { Title = title, UpdatedAt = now };
        titleEditor.Text = title;
        AutomationProperties.SetName(titleEditor, $"Rename {title}");

        if (documentId == _currentDocument.Id)
        {
            _currentDocument = _currentDocument with { Title = title, UpdatedAt = now };
            _titleDisplay.Text = title;
            Title = $"BNP - {title}";
            _autosave.Queue(CreateCurrentSnapshot);
            return;
        }

        var document = _repository.GetDocument(documentId);
        if (document is not null)
        {
            _repository.SaveDocument(document with { Title = title, UpdatedAt = now });
        }
    }

    private void OnTitleEditorKeyDown(Guid documentId, TextBox titleEditor, KeyEventArgs eventArgs)
    {
        if (eventArgs.Key == Key.Enter)
        {
            RenameDocument(documentId, titleEditor);
            _editor.Focus();
            eventArgs.Handled = true;
        }
        else if (eventArgs.Key == Key.Escape)
        {
            var document = _documents.Find(document => document.Id == documentId);
            if (document is not null)
            {
                titleEditor.Text = document.Title;
            }

            _editor.Focus();
            eventArgs.Handled = true;
        }
    }

    private void AttachIconMenu(Button button)
    {
        var menu = new MenuFlyout();
        AddIconMenuItem(menu, "Document", "file-text", LucideIconKind.FileText);
        AddIconMenuItem(menu, "Notebook", "notebook", LucideIconKind.NotebookTabs);
        AddIconMenuItem(menu, "Idea", "idea", LucideIconKind.Lightbulb);
        AddIconMenuItem(menu, "Favorite", "favorite", LucideIconKind.Star);
        AddIconMenuItem(menu, "To-do", "todo", LucideIconKind.ListChecks);
        FlyoutBase.SetAttachedFlyout(button, menu);
        button.Click += (_, _) => FlyoutBase.ShowAttachedFlyout(button);
    }

    private void AttachTextColorMenu(Button button)
    {
        var menu = new MenuFlyout();
        AddTextColorMenuItem(menu, "Automatic", GetAutomaticTextBrush, isAutomatic: true);

        foreach (var (label, color) in TextColorOptions)
        {
            AddTextColorMenuItem(
                menu,
                label,
                () => new SolidColorBrush(Color.Parse(color)));
        }

        FlyoutBase.SetAttachedFlyout(button, menu);
        button.Click += (_, _) => FlyoutBase.ShowAttachedFlyout(button);
    }

    private void AddTextColorMenuItem(
        MenuFlyout menu,
        string label,
        Func<IBrush> getBrush,
        bool isAutomatic = false)
    {
        var brush = getBrush();
        var item = new MenuItem
        {
            Header = label,
            Icon = new Border
            {
                Width = 14,
                Height = 14,
                Background = brush,
                BorderBrush = _palette.Border,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(2)
            }
        };
        item.Click += (_, _) =>
        {
            _editor.Focus();
            _activeTextColor = isAutomatic ? null : getBrush();
            _editor.SetForeground(GetActiveTextBrush());
            UpdateTextColorButton();
        };
        menu.Items.Add(item);
    }

    private void OnEditorKeyDown(object? sender, KeyEventArgs eventArgs)
    {
        if (eventArgs.Key != Key.Enter || eventArgs.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            return;
        }

        var activeTextColor = _activeTextColor;
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            _activeTextColor = activeTextColor;
            _editor.SetForeground(GetActiveTextBrush());
            UpdateTextColorButton();
        });
    }

    private void SyncTextColorFromCaret()
    {
        var foreground = _editor.GetCaretFormat().Foreground;
        _activeTextColor = UsesAutomaticTextColor(foreground) ? null : foreground;
        UpdateTextColorButton();
    }

    private IBrush GetActiveTextBrush()
    {
        return _activeTextColor ?? GetAutomaticTextBrush();
    }

    private void UpdateTextColorButton()
    {
        if (_textColorButton is not null)
        {
            _textColorButton.Foreground = GetActiveTextBrush();
        }
    }

    private void AddIconMenuItem(
        MenuFlyout menu,
        string label,
        string iconKey,
        LucideIconKind iconKind)
    {
        var item = new MenuItem
        {
            Header = label,
            Icon = BnpIcons.Create(iconKind, 16)
        };
        item.Click += (_, _) => SetCurrentDocumentIcon(iconKey);
        menu.Items.Add(item);
    }

    private void SetCurrentDocumentIcon(string iconKey)
    {
        if (_currentDocument.IconKey == iconKey)
        {
            return;
        }

        _currentDocument = _currentDocument with { IconKey = iconKey };
        ReplaceCurrentSummary();
        _autosave.Queue(CreateCurrentSnapshot);
    }

    private void ReplaceCurrentSummary()
    {
        var index = _documents.FindIndex(document => document.Id == _currentDocument.Id);
        if (index >= 0)
        {
            _documents[index] = ToSummary(_currentDocument);
        }

        PopulateDocumentList();
    }

    private void OnWindowKeyDown(object? sender, KeyEventArgs eventArgs)
    {
        if (!eventArgs.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            return;
        }

        if (eventArgs.Key == Key.N)
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
            Content = BnpIcons.Create(icon),
            Width = 32,
            Height = 32,
            Padding = new Thickness(6),
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            CornerRadius = new CornerRadius(5),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        button.PointerEntered += (_, _) => button.Background = _palette.ButtonHover;
        button.PointerExited += (_, _) => button.Background = Brushes.Transparent;
        ToolTip.SetTip(button, accessibleName);
        AutomationProperties.SetName(button, accessibleName);
        return button;
    }

    private Button CreateFormattingButton(
        LucideIconKind icon,
        string accessibleName,
        Action action)
    {
        var button = CreateIconButton(icon, accessibleName);
        button.Focusable = false;
        button.Click += (_, _) => action();
        return button;
    }

    private Border CreateToolbarSeparator()
    {
        return new Border
        {
            Width = 1,
            Height = 20,
            Margin = new Thickness(4, 6),
            Background = _palette.Border,
            VerticalAlignment = VerticalAlignment.Center
        };
    }

    private void ApplyTheme()
    {
        _palette = BnpTheme.GetPalette(ActualThemeVariant);

        Foreground = _palette.PrimaryText;
        _windowFrame.Background = _palette.Window;
        _windowFrame.BorderBrush = _palette.Border;
        _rootLayout.Background = _palette.Window;
        _headerBorder.Background = _palette.Header;
        _headerBorder.BorderBrush = _palette.Border;
        _sidebar.Background = _palette.Sidebar;
        _sidebar.BorderBrush = _palette.Border;
        _toolbarBorder.Background = _palette.Toolbar;
        _toolbarBorder.BorderBrush = _palette.Border;
        _editorSurface.Background = _palette.Editor;
        _statusBorder.Background = _palette.Status;
        _statusBorder.BorderBrush = _palette.Border;
        _titleDisplay.Foreground = _palette.PrimaryText;
        _documentList.Foreground = _palette.PrimaryText;
        _saveStatus.Foreground = _palette.SecondaryText;
        _minimizeButton.Foreground = _palette.PrimaryText;
        _maximizeButton.Foreground = _palette.PrimaryText;
        _closeButton.Foreground = _palette.PrimaryText;
        _editor.CaretBrush = _palette.PrimaryText;
        _editor.SelectionBrush = _palette.Selection;
        ApplyDocumentTextTheme();
        UpdateTextColorButton();
        ApplyWindowState();
    }

    private void ApplyWindowState()
    {
        var isMaximized = WindowState == WindowState.Maximized;
        _maximizeButton.Content = BnpIcons.Create(
            isMaximized ? LucideIconKind.Copy : LucideIconKind.Maximize2,
            16);

        var maximizeLabel = isMaximized ? "Restore window" : "Maximize window";
        ToolTip.SetTip(_maximizeButton, maximizeLabel);
        AutomationProperties.SetName(_maximizeButton, maximizeLabel);
        _windowFrame.BorderThickness = isMaximized ? new Thickness(0) : new Thickness(1);
        _windowFrame.CornerRadius = isMaximized ? new CornerRadius(0) : new CornerRadius(8);
        _windowFrame.ClipToBounds = !isMaximized;
        CornerRadius = isMaximized ? new CornerRadius(0) : new CornerRadius(8);
        ClipToBounds = !isMaximized;
        Win32Properties.SetWindowCornerPreference(
            this,
            isMaximized
                ? Win32Properties.WindowCornerPreference.DoNotRound
                : Win32Properties.WindowCornerPreference.RoundSmall);

        foreach (var zone in _resizeZones)
        {
            zone.IsVisible = !isMaximized;
        }
    }

    private void ApplyDocumentTextTheme()
    {
        if (_editor.Document is not { } document)
        {
            return;
        }

        foreach (var block in document.Blocks)
        {
            ApplyBlockTextTheme(block);
        }

        _editor.InvalidateVisual();
    }

    private void ApplyBlockTextTheme(Block block)
    {
        if (block is Paragraph paragraph)
        {
            foreach (var inline in paragraph.Inlines)
            {
                if (inline is AvaloniaRichEditor.Documents.Run run)
                {
                    if (UsesAutomaticTextColor(run.Foreground))
                    {
                        run.Foreground = GetAutomaticTextBrush();
                    }
                }
                else if (inline is InlineTable inlineTable)
                {
                    ApplyTableTextTheme(inlineTable.Table);
                }
            }
        }
        else if (block is TableBlock table)
        {
            ApplyTableTextTheme(table);
        }
    }

    private IBrush GetAutomaticTextBrush()
    {
        return ActualThemeVariant == ThemeVariant.Dark
            ? Brushes.White
            : _palette.PrimaryText;
    }

    private static bool UsesAutomaticTextColor(IBrush? brush)
    {
        if (brush is not SolidColorBrush solidColorBrush)
        {
            return brush is null;
        }

        return solidColorBrush.Color == Color.Parse("#202020") ||
         solidColorBrush.Color == Color.Parse("#F6F5F4") ||
         solidColorBrush.Color == Colors.White;
    }

    private void ApplyTableTextTheme(TableBlock table)
    {
        foreach (var row in table.Cells)
        {
            foreach (var cell in row)
            {
                foreach (var block in cell.Blocks)
                {
                    ApplyBlockTextTheme(block);
                }
            }
        }
    }

    private static DocumentSummary ToSummary(DocumentRecord document)
    {
        return new DocumentSummary(
            document.Id,
            document.Title,
            document.IconKey,
            document.TabOrder,
            document.UpdatedAt);
    }
}