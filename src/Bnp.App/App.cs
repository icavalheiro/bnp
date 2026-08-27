using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Bnp.Diagnostics;
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
            _repository = new SqliteDocumentRepository(Path.Combine(appDataPath, "bnp.db"));
            var workspace = _repository.Initialize();
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