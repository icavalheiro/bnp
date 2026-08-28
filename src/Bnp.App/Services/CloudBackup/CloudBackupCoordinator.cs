using Avalonia.Threading;
using Bnp.Persistence;

namespace Bnp.Services.CloudBackup;

internal sealed class CloudBackupCoordinator : IDisposable
{
    private static readonly TimeSpan BackupDelay = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan MinimumBackupInterval = TimeSpan.FromMinutes(5);

    private readonly SqliteDocumentRepository _repository;
    private readonly CloudBackupService _backupService;
    private readonly DispatcherTimer _timer = new();
    private readonly CancellationTokenSource _cancellation = new();
    private DateTimeOffset? _lastBackupAt;
    private bool _isBackupRunning;
    private bool _isDisposed;
    private Func<bool>? _flushPendingChanges;
    private Action? _reloadWorkspace;

    public CloudBackupCoordinator(
        SqliteDocumentRepository repository,
        CloudBackupService backupService)
    {
        _repository = repository;
        _backupService = backupService;
        _timer.Tick += OnTimerTick;
        _repository.Changed += OnRepositoryChanged;
    }

    public event Action<bool>? BackupCompleted;

    public void ConfigureMerge(Func<bool> flushPendingChanges, Action reloadWorkspace)
    {
        _flushPendingChanges = flushPendingChanges;
        _reloadWorkspace = reloadWorkspace;
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        var changed = await _backupService.ConnectAsync(
            PrepareForMerge,
            cancellationToken);
        CompleteSuccessfulSync(changed);
    }

    public void Schedule()
    {
        if (_isDisposed || _timer.IsEnabled)
        {
            return;
        }

        try
        {
            if (!_backupService.GetConnectionState().IsConnected)
            {
                return;
            }
        }
        catch
        {
            BackupCompleted?.Invoke(false);
            return;
        }

        var sinceLastBackup = DateTimeOffset.UtcNow - (_lastBackupAt ?? DateTimeOffset.MinValue);
        _timer.Interval = sinceLastBackup >= MinimumBackupInterval
            ? BackupDelay
            : MinimumBackupInterval - sinceLastBackup;
        _timer.Start();
    }

    public void MarkBackupCompleted()
    {
        CompleteSuccessfulSync(databaseChanged: false);
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _repository.Changed -= OnRepositoryChanged;
        _timer.Stop();
        _timer.Tick -= OnTimerTick;
        _cancellation.Cancel();
        _cancellation.Dispose();
        _isDisposed = true;
    }

    private void OnRepositoryChanged()
    {
        Schedule();
    }

    private async void OnTimerTick(object? sender, EventArgs eventArgs)
    {
        if (_isBackupRunning)
        {
            return;
        }

        _timer.Stop();
        _isBackupRunning = true;
        try
        {
            var changed = await _backupService.SynchronizeAsync(
                PrepareForMerge,
                _cancellation.Token);
            CompleteSuccessfulSync(changed);
        }
        catch (OperationCanceledException) when (_cancellation.IsCancellationRequested)
        {
        }
        catch
        {
            BackupCompleted?.Invoke(false);
        }
        finally
        {
            _isBackupRunning = false;
        }
    }

    private void PrepareForMerge()
    {
        if (_flushPendingChanges is not null && !_flushPendingChanges())
        {
            throw new InvalidOperationException("Pending document changes could not be saved before synchronization.");
        }
    }

    private void CompleteSuccessfulSync(bool databaseChanged)
    {
        _lastBackupAt = DateTimeOffset.UtcNow;
        _timer.Stop();
        if (databaseChanged)
        {
            _reloadWorkspace?.Invoke();
        }

        BackupCompleted?.Invoke(true);
    }
}