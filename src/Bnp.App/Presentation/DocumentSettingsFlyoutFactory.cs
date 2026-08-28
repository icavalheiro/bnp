using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Bnp.Core.Documents;
using Bnp.Localization;

namespace Bnp.Presentation;

internal sealed class DocumentSettingsFlyoutFactory(
    EditorCopy copy,
    Func<BnpPalette> getPalette,
    Func<Guid, string?, string, string, bool> saveDocumentSettings)
{
    private EditorCopy _copy = copy;

    private static readonly (string Key, string Color)[] DocumentColorOptions =
    [
        ("slate", "#5B6B82"),
        ("red", "#C43D4F"),
        ("orange", "#D56A28"),
        ("gold", "#B88718"),
        ("green", "#31875B"),
        ("teal", "#25858A"),
        ("blue", "#3678C8"),
        ("indigo", "#575BC7"),
        ("purple", "#8A4FB0"),
        ("pink", "#C04F82")
    ];

    public void ApplyCopy(EditorCopy updatedCopy)
    {
        _copy = updatedCopy;
    }

    public Flyout Create(DocumentSummary document)
    {
        var selectedIconKey = document.IconKey;
        var selectedColorKey = document.ColorKey;
        var titleEditor = new TextBox
        {
            Text = document.Title,
            MinWidth = 310,
            MaxLength = 120
        };
        AutomationProperties.SetName(titleEditor, _copy.DocumentName);

        var preview = new Button
        {
            Content = BnpIcons.CreateDocumentIcon(selectedIconKey, selectedColorKey, 22),
            Width = 40,
            Height = 40,
            Padding = new Thickness(8),
            IsHitTestVisible = false,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center
        };

        var iconButtons = new List<(Button Button, string Key)>();
        var iconPanel = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            ItemWidth = 36,
            ItemHeight = 36
        };
        foreach (var option in BnpIcons.DocumentIcons)
        {
            var iconButton = CreateIconButton(option.Kind, _copy.DocumentIcons[option.Key]);
            iconButton.Width = 34;
            iconButton.Height = 34;
            iconButton.Padding = new Thickness(7);
            iconButtons.Add((iconButton, option.Key));
            iconPanel.Children.Add(iconButton);
            iconButton.Click += (_, _) =>
            {
                selectedIconKey = option.Key;
                preview.Content = BnpIcons.CreateDocumentIcon(selectedIconKey, selectedColorKey, 22);
                RefreshIconSelection();
            };
        }

        var colorButtons = new List<(Button Button, string Color)>();
        var colorPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 7
        };
        foreach (var (key, color) in DocumentColorOptions)
        {
            var colorButton = new Button
            {
                Width = 25,
                Height = 25,
                Padding = new Thickness(4),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Content = new Border
                {
                    Width = 15,
                    Height = 15,
                    Background = new SolidColorBrush(Color.Parse(color)),
                    CornerRadius = new CornerRadius(3)
                }
            };
            ToolTip.SetTip(colorButton, _copy.DocumentColors[key]);
            AutomationProperties.SetName(colorButton, _copy.DocumentColors[key]);
            colorButtons.Add((colorButton, color));
            colorPanel.Children.Add(colorButton);
            colorButton.Click += (_, _) =>
            {
                selectedColorKey = color;
                preview.Content = BnpIcons.CreateDocumentIcon(selectedIconKey, selectedColorKey, 22);
                RefreshColorSelection();
            };
        }

        var flyout = new Flyout();
        var cancelButton = new Button { Content = _copy.Cancel };
        var saveButton = new Button { Content = _copy.Save };
        cancelButton.Click += (_, _) => flyout.Hide();
        saveButton.Click += (_, _) =>
        {
            if (saveDocumentSettings(document.Id, titleEditor.Text, selectedIconKey, selectedColorKey))
            {
                flyout.Hide();
            }
            else
            {
                titleEditor.Focus();
            }
        };
        titleEditor.KeyDown += (_, eventArgs) =>
        {
            if (eventArgs.Key == Key.Enter)
            {
                saveButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                eventArgs.Handled = true;
            }
            else if (eventArgs.Key == Key.Escape)
            {
                flyout.Hide();
                eventArgs.Handled = true;
            }
        };

        var header = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            Children =
            {
                new TextBlock
                {
                    Text = _copy.DocumentSettings,
                    FontSize = 15,
                    FontWeight = FontWeight.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center
                },
                preview
            }
        };
        Grid.SetColumn(preview, 1);

        var footer = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, saveButton }
        };
        flyout.Content = new StackPanel
        {
            Width = 330,
            Spacing = 10,
            Children =
            {
                header,
                new TextBlock { Text = _copy.Name, FontSize = 12, FontWeight = FontWeight.SemiBold },
                titleEditor,
                new TextBlock { Text = _copy.Icon, FontSize = 12, FontWeight = FontWeight.SemiBold },
                new ScrollViewer
                {
                    Content = iconPanel,
                    Height = 180,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                },
                new TextBlock { Text = _copy.Color, FontSize = 12, FontWeight = FontWeight.SemiBold },
                colorPanel,
                footer
            }
        };
        flyout.Opened += (_, _) =>
        {
            titleEditor.SelectAll();
            titleEditor.Focus();
        };
        RefreshIconSelection();
        RefreshColorSelection();
        return flyout;

        void RefreshIconSelection()
        {
            foreach (var (button, key) in iconButtons)
            {
                button.Background = key == selectedIconKey ? getPalette().Selection : Brushes.Transparent;
            }
        }

        void RefreshColorSelection()
        {
            foreach (var (button, color) in colorButtons)
            {
                var palette = getPalette();
                button.BorderBrush = color == selectedColorKey ? palette.PrimaryText : palette.Border;
                button.BorderThickness = color == selectedColorKey ? new Thickness(2) : new Thickness(1);
            }
        }
    }

    private Button CreateIconButton(Lucide.Avalonia.LucideIconKind icon, string accessibleName)
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
        button.PointerEntered += (_, _) => button.Background = getPalette().ButtonHover;
        button.PointerExited += (_, _) => button.Background = Brushes.Transparent;
        ToolTip.SetTip(button, accessibleName);
        AutomationProperties.SetName(button, accessibleName);
        return button;
    }
}