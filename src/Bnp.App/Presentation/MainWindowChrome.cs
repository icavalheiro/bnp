using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Bnp.Localization;
using Lucide.Avalonia;

namespace Bnp.Presentation;

internal sealed class MainWindowChrome
{
    private readonly Window _window;
    private readonly Border _windowFrame;
    private readonly Func<EditorCopy> _getCopy;
    private readonly Func<BnpPalette> _getPalette;
    private readonly Func<bool> _toggleSidebar;
    private readonly Func<string> _getThemeKey;
    private readonly Func<string> _getLanguageKey;
    private readonly Action<string, string> _applyPreferences;
    private readonly Action _openCloudBackupSettings;
    private readonly TextBlock _titleDisplay = new();
    private readonly Button _collapseButton = new();
    private readonly Button _settingsButton = new();
    private readonly Button _minimizeButton = new();
    private readonly Button _maximizeButton = new();
    private readonly Button _closeButton = new();
    private readonly List<Control> _resizeZones = new();

    public MainWindowChrome(
        Window window,
        Border windowFrame,
        Func<EditorCopy> getCopy,
        Func<BnpPalette> getPalette,
        Func<bool> toggleSidebar,
        Func<string> getThemeKey,
        Func<string> getLanguageKey,
        Action<string, string> applyPreferences,
        Action openCloudBackupSettings,
        bool isSidebarCollapsed)
    {
        _window = window;
        _windowFrame = windowFrame;
        _getCopy = getCopy;
        _getPalette = getPalette;
        _toggleSidebar = toggleSidebar;
        _getThemeKey = getThemeKey;
        _getLanguageKey = getLanguageKey;
        _applyPreferences = applyPreferences;
        _openCloudBackupSettings = openCloudBackupSettings;
        Header = BuildHeader(isSidebarCollapsed);
    }

    public Border Header { get; }

    public void SetDocumentTitle(string title)
    {
        _titleDisplay.Text = title;
    }

    public void ToggleSidebar()
    {
        var isCollapsed = _toggleSidebar();
        _collapseButton.Content = BnpIcons.Create(
            isCollapsed ? LucideIconKind.PanelLeftOpen : LucideIconKind.PanelLeftClose);
    }

    public void SetSidebarCollapsed(bool isCollapsed)
    {
        _collapseButton.Content = BnpIcons.Create(
            isCollapsed ? LucideIconKind.PanelLeftOpen : LucideIconKind.PanelLeftClose);
    }

    public void AddResizeZones(Grid rootLayout)
    {
        AddResizeZone(rootLayout, WindowEdge.North, HorizontalAlignment.Stretch,
            VerticalAlignment.Top, height: 6);
        AddResizeZone(rootLayout, WindowEdge.South, HorizontalAlignment.Stretch,
            VerticalAlignment.Bottom, height: 6);
        AddResizeZone(rootLayout, WindowEdge.West, HorizontalAlignment.Left,
            VerticalAlignment.Stretch, width: 6);
        AddResizeZone(rootLayout, WindowEdge.East, HorizontalAlignment.Right,
            VerticalAlignment.Stretch, width: 6);
        AddResizeZone(rootLayout, WindowEdge.NorthWest, HorizontalAlignment.Left,
            VerticalAlignment.Top, 10, 10);
        AddResizeZone(rootLayout, WindowEdge.NorthEast, HorizontalAlignment.Right,
            VerticalAlignment.Top, 10, 10);
        AddResizeZone(rootLayout, WindowEdge.SouthWest, HorizontalAlignment.Left,
            VerticalAlignment.Bottom, 10, 10);
        AddResizeZone(rootLayout, WindowEdge.SouthEast, HorizontalAlignment.Right,
            VerticalAlignment.Bottom, 10, 10);
    }

    public void ApplyTheme()
    {
        var palette = _getPalette();
        Header.Background = palette.Header;
        Header.BorderBrush = palette.Border;
        _titleDisplay.Foreground = palette.PrimaryText;
        _minimizeButton.Foreground = palette.PrimaryText;
        _maximizeButton.Foreground = palette.PrimaryText;
        _closeButton.Foreground = palette.PrimaryText;
        ApplyWindowState();
    }

    public void ApplyCopy()
    {
        var copy = _getCopy();
        SetAccessibleName(_collapseButton, copy.ToggleDocumentSidebar);
        SetAccessibleName(_settingsButton, copy.EditorSettings);
        AutomationProperties.SetName(_titleDisplay, copy.CurrentDocumentTitle);
        SetAccessibleName(_minimizeButton, copy.MinimizeWindow);
        SetAccessibleName(
            _maximizeButton,
            _window.WindowState == WindowState.Maximized
                ? copy.RestoreWindow
                : copy.MaximizeWindow);
        SetAccessibleName(_closeButton, copy.CloseWindow);
    }

    public void ApplyWindowState()
    {
        var isMaximized = _window.WindowState == WindowState.Maximized;
        _maximizeButton.Content = BnpIcons.Create(
            isMaximized ? LucideIconKind.Copy : LucideIconKind.Maximize2,
            16);

        var copy = _getCopy();
        var maximizeLabel = isMaximized ? copy.RestoreWindow : copy.MaximizeWindow;
        ToolTip.SetTip(_maximizeButton, maximizeLabel);
        AutomationProperties.SetName(_maximizeButton, maximizeLabel);
        _windowFrame.BorderThickness = isMaximized ? new Thickness(0) : new Thickness(1);
        _windowFrame.CornerRadius = isMaximized ? new CornerRadius(0) : new CornerRadius(8);
        _windowFrame.ClipToBounds = !isMaximized;
        _window.CornerRadius = isMaximized ? new CornerRadius(0) : new CornerRadius(8);
        _window.ClipToBounds = !isMaximized;
        Win32Properties.SetWindowCornerPreference(
            _window,
            isMaximized
                ? Win32Properties.WindowCornerPreference.DoNotRound
                : Win32Properties.WindowCornerPreference.RoundSmall);

        foreach (var zone in _resizeZones)
        {
            zone.IsVisible = !isMaximized;
        }
    }

    private Border BuildHeader(bool isSidebarCollapsed)
    {
        ConfigureIconButton(
            _collapseButton,
            isSidebarCollapsed ? LucideIconKind.PanelLeftOpen : LucideIconKind.PanelLeftClose,
            _getCopy().ToggleDocumentSidebar);
        _collapseButton.Click += (_, _) => ToggleSidebar();

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
        AutomationProperties.SetName(_titleDisplay, _getCopy().CurrentDocumentTitle);

        var brand = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 9,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                _collapseButton,
                new Image
                {
                    Source = new Bitmap(AssetLoader.Open(new Uri("avares://BNP/Assets/BNP.ico"))),
                    Width = 19,
                    Height = 19,
                    Stretch = Stretch.Uniform
                }
            }
        };
        var captionButtons = BuildCaptionButtons();
        ConfigureIconButton(_settingsButton, LucideIconKind.Settings, _getCopy().EditorSettings);
        _settingsButton.Click += (_, eventArgs) =>
        {
            var flyout = EditorSettingsFlyoutFactory.Create(
                _getCopy(),
                _getThemeKey(),
                _getLanguageKey(),
                _applyPreferences,
                _openCloudBackupSettings);
            FlyoutBase.SetAttachedFlyout(_settingsButton, flyout);
            FlyoutBase.ShowAttachedFlyout(_settingsButton);
            eventArgs.Handled = true;
        };
        var rightActions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Stretch,
            Children = { _settingsButton, captionButtons }
        };
        var headerLayout = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto,*"),
            Background = Brushes.Transparent,
            Children = { brand, _titleDisplay, rightActions }
        };
        Grid.SetColumn(_titleDisplay, 1);
        Grid.SetColumn(rightActions, 2);
        headerLayout.PointerPressed += OnTitleBarPointerPressed;

        return new Border
        {
            MinHeight = 44,
            Padding = new Thickness(8, 0, 0, 0),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = headerLayout
        };
    }

    private StackPanel BuildCaptionButtons()
    {
        ConfigureCaptionButton(_minimizeButton, LucideIconKind.Minus, _getCopy().MinimizeWindow);
        _minimizeButton.Click += (_, _) => _window.WindowState = WindowState.Minimized;

        ConfigureCaptionButton(_maximizeButton, LucideIconKind.Maximize2, _getCopy().MaximizeWindow);
        _maximizeButton.Click += (_, _) => ToggleMaximize();

        ConfigureCaptionButton(_closeButton, LucideIconKind.X, _getCopy().CloseWindow);
        _closeButton.Click += (_, _) => _window.Close();
        _closeButton.PointerEntered += (_, _) =>
        {
            _closeButton.Background = new SolidColorBrush(Color.Parse("#C42B1C"));
            _closeButton.Foreground = Brushes.White;
        };
        _closeButton.PointerExited += (_, _) =>
        {
            _closeButton.Background = Brushes.Transparent;
            _closeButton.Foreground = _getPalette().PrimaryText;
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
        button.PointerEntered += (_, _) => button.Background = _getPalette().ButtonHover;
        button.PointerExited += (_, _) => button.Background = Brushes.Transparent;
        ToolTip.SetTip(button, accessibleName);
        AutomationProperties.SetName(button, accessibleName);
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
        button.PointerEntered += (_, _) => button.Background = _getPalette().ButtonHover;
        button.PointerExited += (_, _) => button.Background = Brushes.Transparent;
        SetAccessibleName(button, accessibleName);
    }

    private static void SetAccessibleName(Button button, string accessibleName)
    {
        ToolTip.SetTip(button, accessibleName);
        AutomationProperties.SetName(button, accessibleName);
    }

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs eventArgs)
    {
        if (!eventArgs.GetCurrentPoint(_window).Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (eventArgs.ClickCount == 2 && _window.CanMaximize)
        {
            ToggleMaximize();
            eventArgs.Handled = true;
            return;
        }

        _window.BeginMoveDrag(eventArgs);
        eventArgs.Handled = true;
    }

    private void ToggleMaximize()
    {
        _window.WindowState = _window.WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void AddResizeZone(
        Grid rootLayout,
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
            if (_window.WindowState == WindowState.Normal &&
                eventArgs.GetCurrentPoint(_window).Properties.IsLeftButtonPressed)
            {
                _window.BeginResizeDrag(edge, eventArgs);
                eventArgs.Handled = true;
            }
        };
        Grid.SetColumnSpan(zone, 2);
        Grid.SetRowSpan(zone, 3);
        rootLayout.Children.Add(zone);
        _resizeZones.Add(zone);
    }
}