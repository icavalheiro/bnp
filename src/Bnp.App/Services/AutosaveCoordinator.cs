using Avalonia.Threading;
using Bnp.Core.Documents;

namespace Bnp.Services;

public sealed class AutosaveCoordinator : IDisposable
{
    private readonly IDocumentRepository _repository;
    private readonly DispatcherTimer _timer;
    private Func<DocumentRecord>? _pendingSnapshot;
    private bool _isDisposed;

    public AutosaveCoordinator(IDocumentRepository repository, TimeSpan delay)
    {
        _repository = repository;
        _timer = new DispatcherTimer { Interval = delay };
        _timer.Tick += OnTimerTick;
    }

    public event Action<SaveStatus>? StatusChanged;

    public void Queue(Func<DocumentRecord> snapshotFactory)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        ArgumentNullException.ThrowIfNull(snapshotFactory);

        _pendingSnapshot = snapshotFactory;
        _timer.Stop();
        _timer.Start();
        StatusChanged?.Invoke(SaveStatus.Unsaved);
    }

    public bool Flush()
    {
        if (_pendingSnapshot is null)
        {
            return true;
        }

        _timer.Stop();
        StatusChanged?.Invoke(SaveStatus.Saving);

        try
        {
            var snapshot = _pendingSnapshot();
            _repository.SaveDocument(snapshot);
            _pendingSnapshot = null;
            StatusChanged?.Invoke(SaveStatus.Saved);
            return true;
        }
        catch
        {
            StatusChanged?.Invoke(SaveStatus.Failed);
            return false;
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        Flush();
        _timer.Stop();
        _timer.Tick -= OnTimerTick;
        _isDisposed = true;
    }

    private void OnTimerTick(object? sender, EventArgs eventArgs)
    {
        Flush();
    }
}