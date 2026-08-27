using System.Diagnostics;
using System.Globalization;
using System.IO.Pipes;

const double startupTargetMilliseconds = 200;

if (args.Length is < 1 or > 2)
{
    Console.Error.WriteLine("Usage: Bnp.StartupBench <executable> [iterations]");
    return 1;
}

var executablePath = Path.GetFullPath(args[0]);
if (!File.Exists(executablePath))
{
    Console.Error.WriteLine($"Executable not found: {executablePath}");
    return 1;
}

var iterations = args.Length == 2
    ? int.Parse(args[1], CultureInfo.InvariantCulture)
    : 30;
if (iterations < 1)
{
    Console.Error.WriteLine("Iterations must be greater than zero.");
    return 1;
}

var externalMeasurements = new List<double>(iterations);
var internalMeasurements = new List<double>(iterations);
var databaseMeasurements = new List<double>(iterations);
var windowMeasurements = new List<double>(iterations);

for (var iteration = 1; iteration <= iterations; iteration++)
{
    var pipeName = $"bnp-startup-{Guid.NewGuid():N}";
    await using var pipe = new NamedPipeServerStream(
        pipeName,
        PipeDirection.In,
        1,
        PipeTransmissionMode.Byte,
        PipeOptions.Asynchronous);

    var startInfo = new ProcessStartInfo(executablePath)
    {
        UseShellExecute = false,
        WorkingDirectory = Path.GetDirectoryName(executablePath)!
    };
    startInfo.Environment["BNP_STARTUP_PIPE"] = pipeName;

    var stopwatch = Stopwatch.StartNew();
    using var process = Process.Start(startInfo)
        ?? throw new InvalidOperationException("The BNP process could not be started.");

    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
    try
    {
        await pipe.WaitForConnectionAsync(timeout.Token);
        using var reader = new BinaryReader(pipe);
        var internalMilliseconds = reader.ReadDouble();
        var databaseMilliseconds = reader.ReadDouble();
        var windowMilliseconds = reader.ReadDouble();
        stopwatch.Stop();

        internalMeasurements.Add(internalMilliseconds);
        databaseMeasurements.Add(databaseMilliseconds);
        windowMeasurements.Add(windowMilliseconds);
        externalMeasurements.Add(stopwatch.Elapsed.TotalMilliseconds);
        Console.WriteLine(
            $"{iteration,2}: external={stopwatch.Elapsed.TotalMilliseconds,8:F2} ms  " +
            $"internal={internalMilliseconds,8:F2} ms  database={databaseMilliseconds,8:F2} ms  " +
            $"window={windowMilliseconds,8:F2} ms");
    }
    finally
    {
        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
        }

        await process.WaitForExitAsync();
    }
}

externalMeasurements.Sort();
internalMeasurements.Sort();
databaseMeasurements.Sort();
windowMeasurements.Sort();

var externalP50 = Percentile(externalMeasurements, 0.50);
var externalP95 = Percentile(externalMeasurements, 0.95);
var internalP50 = Percentile(internalMeasurements, 0.50);
var internalP95 = Percentile(internalMeasurements, 0.95);
var databaseP95 = Percentile(databaseMeasurements, 0.95);
var windowP95 = Percentile(windowMeasurements, 0.95);

Console.WriteLine();
Console.WriteLine($"External p50: {externalP50:F2} ms");
Console.WriteLine($"External p95: {externalP95:F2} ms");
Console.WriteLine($"Internal p50: {internalP50:F2} ms");
Console.WriteLine($"Internal p95: {internalP95:F2} ms");
Console.WriteLine($"Database ready p95: {databaseP95:F2} ms");
Console.WriteLine($"Window constructed p95: {windowP95:F2} ms");
Console.WriteLine($"Target: p95 <= {startupTargetMilliseconds:F0} ms");

return externalP95 <= startupTargetMilliseconds ? 0 : 2;

static double Percentile(IReadOnlyList<double> sortedValues, double percentile)
{
    var index = Math.Clamp(
        (int)Math.Ceiling(percentile * sortedValues.Count) - 1,
        0,
        sortedValues.Count - 1);
    return sortedValues[index];
}