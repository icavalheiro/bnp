using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Bnp.Localization;

namespace Bnp.Presentation;

internal static class EditorSettingsFlyoutFactory
{
    public static Flyout Create(
        EditorCopy copy,
        string selectedThemeKey,
        string selectedLanguageKey,
        Action<string, string> applyPreferences)
    {
        var themeOptions = new[]
        {
            new SettingOption("system", copy.SystemTheme),
            new SettingOption("light", copy.LightTheme),
            new SettingOption("dark", copy.DarkTheme)
        };
        var languageOptions = new[]
        {
            new SettingOption("es", copy.Spanish),
            new SettingOption("pt", copy.Portuguese),
            new SettingOption("fr", copy.French),
            new SettingOption("en", copy.English)
        };
        var themeSelector = CreateSelector(themeOptions, selectedThemeKey, copy.Theme);
        var languageSelector = CreateSelector(languageOptions, selectedLanguageKey, copy.Language);
        var flyout = new Flyout();
        var applyButton = new Button
        {
            Content = copy.Apply,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        applyButton.Click += (_, _) =>
        {
            if (themeSelector.SelectedItem is SettingOption theme &&
                languageSelector.SelectedItem is SettingOption language)
            {
                applyPreferences(theme.Key, language.Key);
                flyout.Hide();
            }
        };

        flyout.Content = new StackPanel
        {
            Width = 260,
            Spacing = 10,
            Children =
            {
                new TextBlock
                {
                    Text = copy.EditorSettings,
                    FontSize = 15,
                    FontWeight = Avalonia.Media.FontWeight.SemiBold
                },
                new TextBlock { Text = copy.Theme, FontSize = 12 },
                themeSelector,
                new TextBlock { Text = copy.Language, FontSize = 12 },
                languageSelector,
                applyButton
            }
        };
        return flyout;
    }

    private static ComboBox CreateSelector(
        IReadOnlyList<SettingOption> options,
        string selectedKey,
        string accessibleName)
    {
        var selector = new ComboBox
        {
            ItemsSource = options,
            SelectedItem = options.FirstOrDefault(option => option.Key == selectedKey) ?? options[0],
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        AutomationProperties.SetName(selector, accessibleName);
        return selector;
    }

    private sealed record SettingOption(string Key, string Label)
    {
        public override string ToString() => Label;
    }
}