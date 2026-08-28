using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using AvaloniaRichEditor.Controls;
using Bnp.Localization;
using Lucide.Avalonia;

namespace Bnp.Presentation;

internal sealed class EditorFormattingToolbar
{
    private static readonly (string Key, string Color)[] TextColorOptions =
    [
        ("red", "#E53935"),
        ("orange", "#FB8C00"),
        ("yellow", "#FDD835"),
        ("green", "#43A047"),
        ("teal", "#00897B"),
        ("blue", "#1E88E5"),
        ("purple", "#8E24AA"),
        ("gray", "#616161")
    ];

    private readonly RichEditor _editor;
    private EditorCopy _copy;
    private readonly Func<IBrush> _getAutomaticTextBrush;
    private readonly Func<BnpPalette> _getPalette;
    private Button _textColorButton = null!;
    private Button _clearFormattingButton = null!;
    private readonly List<Border> _separators = new();
    private IBrush? _activeTextColor;

    public EditorFormattingToolbar(
        RichEditor editor,
        EditorCopy copy,
        Func<IBrush> getAutomaticTextBrush,
        Func<BnpPalette> getPalette)
    {
        _editor = editor;
        _copy = copy;
        _getAutomaticTextBrush = getAutomaticTextBrush;
        _getPalette = getPalette;

        View = new Border
        {
            MinHeight = 43,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = BuildToolbar()
        };

        _editor.SelectionChanged += (_, _) => SyncFromCaret();
        _editor.AddHandler(
            InputElement.KeyDownEvent,
            OnEditorKeyDown,
            RoutingStrategies.Tunnel,
            handledEventsToo: true);
        UpdateTextColorButton();
    }

    public Border View { get; }

    public void ApplyCopy(EditorCopy copy)
    {
        _copy = copy;
        _separators.Clear();
        View.Child = BuildToolbar();
        ApplyPalette();
        SyncFromCaret();
    }

    public void ApplyPalette()
    {
        var palette = _getPalette();
        View.Background = palette.Toolbar;
        View.BorderBrush = palette.Border;
        foreach (var separator in _separators)
        {
            separator.Background = palette.Border;
        }

        UpdateTextColorButton();
    }

    public void SyncFromCaret()
    {
        var foreground = _editor.GetCaretFormat().Foreground;
        _activeTextColor = RichEditorThemeApplicator.UsesAutomaticTextColor(foreground)
            ? null
            : foreground;
        UpdateTextColorButton();
        _clearFormattingButton.IsEnabled = RichEditorFormattingAdapter.HasSelection(_editor);
    }

    private StackPanel BuildToolbar()
    {
        _textColorButton = CreateIconButton(LucideIconKind.Palette, _copy.TextColor);
        AttachTextColorMenu(_textColorButton);
        _clearFormattingButton = CreateFormattingButton(
            LucideIconKind.RemoveFormatting,
            _copy.ClearFormatting,
            ClearSelectedFormatting);

        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 3,
            Margin = new Thickness(10, 5),
            Children =
            {
                CreateFormattingButton(LucideIconKind.Undo2, _copy.Undo, _editor.Undo),
                CreateFormattingButton(LucideIconKind.Redo2, _copy.Redo, _editor.Redo),
                CreateSeparator(),
                CreateFormattingButton(LucideIconKind.Bold, _copy.Bold, _editor.ToggleBold),
                CreateFormattingButton(LucideIconKind.Italic, _copy.Italic, _editor.ToggleItalic),
                CreateFormattingButton(LucideIconKind.Highlighter, _copy.Highlight, ToggleHighlight),
                _textColorButton,
                _clearFormattingButton,
                CreateSeparator(),
                CreateFormattingButton(
                    LucideIconKind.TextAlignStart,
                    _copy.AlignLeft,
                    () => _editor.SetTextAlignment(TextAlignment.Left)),
                CreateFormattingButton(
                    LucideIconKind.TextAlignCenter,
                    _copy.AlignCenter,
                    () => _editor.SetTextAlignment(TextAlignment.Center)),
                CreateFormattingButton(
                    LucideIconKind.TextAlignEnd,
                    _copy.AlignRight,
                    () => _editor.SetTextAlignment(TextAlignment.Right))
            }
        };
    }

    private void AttachTextColorMenu(Button button)
    {
        var menu = new MenuFlyout();
        AddTextColorMenuItem(menu, _copy.Automatic, _getAutomaticTextBrush, isAutomatic: true);

        foreach (var (key, color) in TextColorOptions)
        {
            AddTextColorMenuItem(
                menu,
                _copy.TextColors[key],
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
        var item = new MenuItem
        {
            Header = label,
            Icon = new Border
            {
                Width = 14,
                Height = 14,
                Background = getBrush(),
                BorderBrush = _getPalette().Border,
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

    private void ToggleHighlight()
    {
        var background = _editor.GetCaretFormat().Background;
        var isHighlighted = background is SolidColorBrush { Color.A: > 0 };
        _editor.SetHighlight(isHighlighted ? Brushes.Transparent : _getPalette().Highlight);
    }

    private void ClearSelectedFormatting()
    {
        if (!RichEditorFormattingAdapter.HasSelection(_editor))
        {
            return;
        }

        RichEditorFormattingAdapter.ClearFormatting(_editor, _getAutomaticTextBrush());
        SyncFromCaret();
    }

    private IBrush GetActiveTextBrush()
    {
        return _activeTextColor ?? _getAutomaticTextBrush();
    }

    private void UpdateTextColorButton()
    {
        _textColorButton.Foreground = GetActiveTextBrush();
    }

    private Button CreateFormattingButton(
        LucideIconKind icon,
        string accessibleName,
        Action action)
    {
        var button = CreateIconButton(icon, accessibleName);
        button.Click += (_, _) => action();
        return button;
    }

    private Button CreateIconButton(LucideIconKind icon, string accessibleName)
    {
        var button = new Button
        {
            Content = BnpIcons.Create(icon),
            Width = 32,
            Height = 32,
            Padding = new Thickness(6),
            Focusable = false,
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            CornerRadius = new CornerRadius(5),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        button.PointerEntered += (_, _) => button.Background = _getPalette().ButtonHover;
        button.PointerExited += (_, _) => button.Background = Brushes.Transparent;
        ToolTip.SetTip(button, accessibleName);
        AutomationProperties.SetName(button, accessibleName);
        return button;
    }

    private Border CreateSeparator()
    {
        var separator = new Border
        {
            Width = 1,
            Height = 20,
            Margin = new Thickness(4, 6),
            Background = _getPalette().Border,
            VerticalAlignment = VerticalAlignment.Center
        };
        _separators.Add(separator);
        return separator;
    }
}