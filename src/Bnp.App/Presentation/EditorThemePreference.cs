using Avalonia.Styling;

namespace Bnp.Presentation;

internal static class EditorThemePreference
{
    public static ThemeVariant ToThemeVariant(string themeKey)
    {
        return themeKey switch
        {
            "light" => ThemeVariant.Light,
            "dark" => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };
    }
}