using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Bnp.Diagnostics;
using Bnp.Localization;
using Bnp.Persistence;
using Bnp.Presentation;
using Bnp.Services.CloudBackup;

namespace Bnp;

public sealed class App : Application, IDisposable
{
    private SqliteDocumentRepository? _repository;
    private CloudBackupService? _cloudBackupService;
    private CloudBackupCoordinator? _cloudBackupCoordinator;

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
            _cloudBackupService = new CloudBackupService(
                _repository,
                Path.Combine(appDataPath, "cloud-backup.json"));
            _cloudBackupCoordinator = new CloudBackupCoordinator(
                _repository,
                _cloudBackupService);
            StartupMetrics.MarkDatabaseReady();
            desktop.MainWindow = new MainWindow(
                _repository,
                workspace,
                _cloudBackupService,
                _cloudBackupCoordinator);
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
        _cloudBackupCoordinator?.Dispose();
        _cloudBackupCoordinator = null;
        _cloudBackupService?.Dispose();
        _cloudBackupService = null;
        _repository?.Dispose();
        _repository = null;
    }
}