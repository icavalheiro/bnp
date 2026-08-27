using Avalonia.Media;
using Avalonia.Styling;

namespace Bnp.Presentation;

internal sealed record BnpPalette(
    IBrush Window,
    IBrush Header,
    IBrush Sidebar,
    IBrush Editor,
    IBrush Toolbar,
    IBrush Status,
    IBrush Border,
    IBrush PrimaryText,
    IBrush SecondaryText,
    IBrush ButtonHover,
    IBrush Selection,
    IBrush Highlight);

internal static class BnpTheme
{
    private static readonly BnpPalette Dark = new(
        Brush("#202020"),
        Brush("#303030"),
        Brush("#282828"),
        Brush("#242424"),
        Brush("#2B2B2B"),
        Brush("#2B2B2B"),
        Brush("#454545"),
        Brush("#F6F5F4"),
        Brush("#B8B8B8"),
        Brush("#414141"),
        Brush("#663584E4"),
        Brush("#B58B24"));

    private static readonly BnpPalette Light = new(
        Brush("#F6F5F4"),
        Brush("#EBEAE8"),
        Brush("#F2F1EF"),
        Brush("#FFFFFF"),
        Brush("#F7F6F5"),
        Brush("#EBEAE8"),
        Brush("#D4D2CF"),
        Brush("#202020"),
        Brush("#666666"),
        Brush("#DDDAD6"),
        Brush("#553584E4"),
        Brush("#F4D35E"));

    public static BnpPalette GetPalette(ThemeVariant actualThemeVariant)
    {
        return actualThemeVariant == ThemeVariant.Dark ? Dark : Light;
    }

    private static SolidColorBrush Brush(string color)
    {
        return new SolidColorBrush(Color.Parse(color));
    }
}