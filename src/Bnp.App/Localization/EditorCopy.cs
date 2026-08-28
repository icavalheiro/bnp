using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bnp.Localization;

internal sealed class EditorCopy
{
    public string ApplicationTitle { get; init; } = string.Empty;
    public string WindowTitle { get; init; } = string.Empty;
    public string DocumentEditor { get; init; } = string.Empty;
    public string NewDocument { get; init; } = string.Empty;
    public string Documents { get; init; } = string.Empty;
    public string OpenDocuments { get; init; } = string.Empty;
    public string ConfigureDocument { get; init; } = string.Empty;
    public string UntitledDocument { get; init; } = string.Empty;
    public string WelcomeDocumentTitle { get; init; } = string.Empty;
    public string WelcomeDocumentContent { get; init; } = string.Empty;
    public string Saved { get; init; } = string.Empty;
    public string Unsaved { get; init; } = string.Empty;
    public string Saving { get; init; } = string.Empty;
    public string SaveFailed { get; init; } = string.Empty;
    public string DocumentUnavailable { get; init; } = string.Empty;
    public string TextColor { get; init; } = string.Empty;
    public string ClearFormatting { get; init; } = string.Empty;
    public string Undo { get; init; } = string.Empty;
    public string Redo { get; init; } = string.Empty;
    public string Bold { get; init; } = string.Empty;
    public string Italic { get; init; } = string.Empty;
    public string Highlight { get; init; } = string.Empty;
    public string AlignLeft { get; init; } = string.Empty;
    public string AlignCenter { get; init; } = string.Empty;
    public string AlignRight { get; init; } = string.Empty;
    public string Automatic { get; init; } = string.Empty;
    public string DocumentName { get; init; } = string.Empty;
    public string Cancel { get; init; } = string.Empty;
    public string Save { get; init; } = string.Empty;
    public string DocumentSettings { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Icon { get; init; } = string.Empty;
    public string Color { get; init; } = string.Empty;
    public string RestoreWindow { get; init; } = string.Empty;
    public string MaximizeWindow { get; init; } = string.Empty;
    public string ToggleDocumentSidebar { get; init; } = string.Empty;
    public string CurrentDocumentTitle { get; init; } = string.Empty;
    public string MinimizeWindow { get; init; } = string.Empty;
    public string CloseWindow { get; init; } = string.Empty;
    public string EditorSettings { get; init; } = string.Empty;
    public string Theme { get; init; } = string.Empty;
    public string Language { get; init; } = string.Empty;
    public string SystemTheme { get; init; } = string.Empty;
    public string LightTheme { get; init; } = string.Empty;
    public string DarkTheme { get; init; } = string.Empty;
    public string Spanish { get; init; } = string.Empty;
    public string Portuguese { get; init; } = string.Empty;
    public string French { get; init; } = string.Empty;
    public string English { get; init; } = string.Empty;
    public string Apply { get; init; } = string.Empty;
    public string CloudBackups { get; init; } = string.Empty;
    public string CloudService { get; init; } = string.Empty;
    public string Dropbox { get; init; } = string.Empty;
    public string ConnectAndBackup { get; init; } = string.Empty;
    public string Disconnect { get; init; } = string.Empty;
    public string CloudConnecting { get; init; } = string.Empty;
    public string CloudConnected { get; init; } = string.Empty;
    public string CloudDisconnected { get; init; } = string.Empty;
    public string CloudBackupFailed { get; init; } = string.Empty;
    public Dictionary<string, string> TextColors { get; init; } = [];
    public Dictionary<string, string> DocumentColors { get; init; } = [];
    public Dictionary<string, string> DocumentIcons { get; init; } = [];
}

internal static class EditorCopyCatalog
{
    private const string DefaultLanguage = "en";
    private static readonly HashSet<string> SupportedLanguages =
        new(StringComparer.OrdinalIgnoreCase) { "en", "pt", "es", "fr" };

    public static EditorCopy Load(CultureInfo culture)
    {
        return Load(ResolveLanguage(culture));
    }

    public static string ResolveLanguage(CultureInfo culture)
    {
        return SupportedLanguages.Contains(culture.TwoLetterISOLanguageName)
            ? culture.TwoLetterISOLanguageName.ToLowerInvariant()
            : DefaultLanguage;
    }

    public static EditorCopy Load(string languageKey)
    {
        var language = SupportedLanguages.Contains(languageKey) ? languageKey : DefaultLanguage;
        var resourceName = $"BNP.Localization.copy.{language}.json";
        using var stream = typeof(EditorCopyCatalog).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Missing embedded copy resource '{resourceName}'.");
        return JsonSerializer.Deserialize(stream, EditorCopyJsonContext.Default.EditorCopy)
            ?? throw new InvalidOperationException($"Copy resource '{resourceName}' is empty.");
    }
}

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(EditorCopy))]
internal sealed partial class EditorCopyJsonContext : JsonSerializerContext;