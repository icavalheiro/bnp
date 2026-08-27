using System.Diagnostics;
using System.IO.Pipes;

namespace Bnp.Diagnostics;

internal static class StartupMetrics
{
    private static long _startedAt;
    private static double _databaseReadyMilliseconds;
    private static double _windowReadyMilliseconds;
    private static int _isReady;

    public static void Start()
    {
        _startedAt = Stopwatch.GetTimestamp();
    }

    public static void MarkReady()
    {
        if (Interlocked.Exchange(ref _isReady, 1) != 0)
        {
            return;
        }

        var pipeName = Environment.GetEnvironmentVariable("BNP_STARTUP_PIPE");
        if (string.IsNullOrWhiteSpace(pipeName))
        {
            return;
        }

        var elapsed = Stopwatch.GetElapsedTime(_startedAt);
        using var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.Out);
        pipe.Connect(2_000);
        using var writer = new BinaryWriter(pipe);
        writer.Write(elapsed.TotalMilliseconds);
        writer.Write(_databaseReadyMilliseconds);
        writer.Write(_windowReadyMilliseconds);
        writer.Flush();
    }

    public static void MarkDatabaseReady()
    {
        _databaseReadyMilliseconds = Stopwatch.GetElapsedTime(_startedAt).TotalMilliseconds;
    }

    public static void MarkWindowReady()
    {
        _windowReadyMilliseconds = Stopwatch.GetElapsedTime(_startedAt).TotalMilliseconds;
    }
}