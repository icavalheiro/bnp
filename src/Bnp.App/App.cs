using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Bnp.Diagnostics;
using Bnp.Localization;
using Bnp.Persistence;
using Bnp.Presentation;

namespace Bnp;

public sealed class App : Application, IDisposable
{
    private SqliteDocumentRepository? _repository;

    public override void Initialize()
    {
        RequestedThemeVariant = ThemeVariant.Default;
        Styles.Add(new FluentTheme { DensityStyle = DensityStyle.Compact });
        BnpIcons.ConfigureRichEditorIcons();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BNP");
            var copy = EditorCopyCatalog.Load(System.Globalization.CultureInfo.CurrentUICulture);
            _repository = new SqliteDocumentRepository(Path.Combine(appDataPath, "bnp.db"));
            var workspace = _repository.Initialize(
                copy.WelcomeDocumentTitle,
                copy.WelcomeDocumentContent);
            if (string.IsNullOrEmpty(workspace.LanguageKey))
            {
                var languageKey = EditorCopyCatalog.ResolveLanguage(
                    System.Globalization.CultureInfo.CurrentUICulture);
                _repository.SetEditorPreferences(workspace.ThemeKey, languageKey);
                workspace = workspace with { LanguageKey = languageKey };
            }

            RequestedThemeVariant = EditorThemePreference.ToThemeVariant(workspace.ThemeKey);
            StartupMetrics.MarkDatabaseReady();
            desktop.MainWindow = new MainWindow(_repository, workspace);
            StartupMetrics.MarkWindowReady();
            desktop.Exit += OnDesktopExit;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnDesktopExit(object? sender, ControlledApplicationLifetimeExitEventArgs eventArgs)
    {
        Dispose();
    }

    public void Dispose()
    {
        _repository?.Dispose();
        _repository = null;
    }
}