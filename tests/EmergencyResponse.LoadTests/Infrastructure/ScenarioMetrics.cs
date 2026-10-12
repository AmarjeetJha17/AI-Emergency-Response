using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;

namespace EmergencyResponse.LoadTests.Infrastructure;

public sealed class ScenarioMetrics
{
    private readonly ConcurrentDictionary<string, MetricCounter> _counters = new(StringComparer.OrdinalIgnoreCase);

    public long TotalRequests => _counters.Values.Sum(counter => Interlocked.Read(ref counter.Requests));
    public long TotalFailures => _counters.Values.Sum(counter => Interlocked.Read(ref counter.Failures));
    public double ErrorRate => TotalRequests == 0 ? 0 : (double)TotalFailures / TotalRequests;
    public void Record(string name, TimeSpan elapsed, bool succeeded)
    {
        var counter = _counters.GetOrAdd(name, static _ => new MetricCounter());
        Interlocked.Increment(ref counter.Requests);
        if (!succeeded)
        {
            Interlocked.Increment(ref counter.Failures);
        }
        var sampleIndex = Interlocked.Increment(ref counter.SampleCount) - 1;
        if (sampleIndex < counter.Latencies.Length)
        {
            counter.Latencies[sampleIndex] = elapsed.TotalMilliseconds;
        }
    }

    public object Snapshot()
    {
        return _counters.OrderBy(pair => pair.Key).Select(pair =>
        {
            var count = Math.Min(Volatile.Read(ref pair.Value.SampleCount), pair.Value.Latencies.Length);
            var latencies = pair.Value.Latencies.Take(count).ToArray();
            Array.Sort(latencies);
            return new
            {
                scenario = pair.Key,
                requests = Interlocked.Read(ref pair.Value.Requests),
                failures = Interlocked.Read(ref pair.Value.Failures),
                p50Ms = Percentile(latencies, 0.50),
                p95Ms = Percentile(latencies, 0.95),
                p99Ms = Percentile(latencies, 0.99)
            };
        }).ToArray();
    }

    private static double Percentile(double[] sorted, double percentile)
    {
        if (sorted.Length == 0)
        {
            return 0;
        }

        var index = Math.Min(sorted.Length - 1, (int)Math.Ceiling(percentile * sorted.Length) - 1);
        return Math.Round(sorted[index], 2);
    }

    private sealed class MetricCounter
    {
        public long Requests;
        public long Failures;
        public double[] Latencies { get; } = new double[250_000];
        public int SampleCount;
    }

}

public sealed class LoadTestFaultController
{
    private int _aiUnavailable;
    private int _searchUnavailable;
    private int _weatherUnavailable;
    private int _storageUnavailable;
    private int _publishUnavailable;
    private int _databaseFailure;
    private int _databaseDelayMs;
    private int _aiDelayMs;
    private long _databaseCommandCount;
    private long _injectedDatabaseDelayMs;
    private long _sqlFailureCount;

    public bool AIUnavailable { get => Volatile.Read(ref _aiUnavailable) != 0; set => Volatile.Write(ref _aiUnavailable, value ? 1 : 0); }
    public bool SearchUnavailable { get => Volatile.Read(ref _searchUnavailable) != 0; set => Volatile.Write(ref _searchUnavailable, value ? 1 : 0); }
    public bool WeatherUnavailable { get => Volatile.Read(ref _weatherUnavailable) != 0; set => Volatile.Write(ref _weatherUnavailable, value ? 1 : 0); }
    public bool StorageUnavailable { get => Volatile.Read(ref _storageUnavailable) != 0; set => Volatile.Write(ref _storageUnavailable, value ? 1 : 0); }
    public bool PublishUnavailable { get => Volatile.Read(ref _publishUnavailable) != 0; set => Volatile.Write(ref _publishUnavailable, value ? 1 : 0); }
    public bool DatabaseFailure { get => Volatile.Read(ref _databaseFailure) != 0; set => Volatile.Write(ref _databaseFailure, value ? 1 : 0); }
    public int DatabaseDelayMs { get => Volatile.Read(ref _databaseDelayMs); set => Volatile.Write(ref _databaseDelayMs, Math.Clamp(value, 0, 10_000)); }
    public int AIDelayMs { get => Volatile.Read(ref _aiDelayMs); set => Volatile.Write(ref _aiDelayMs, Math.Clamp(value, 0, 60_000)); }
    public long DatabaseCommandCount => Interlocked.Read(ref _databaseCommandCount);
    public long InjectedDatabaseDelayMs => Interlocked.Read(ref _injectedDatabaseDelayMs);
    public long SqlFailureCount => Interlocked.Read(ref _sqlFailureCount);
    public object SqlSnapshot() => _sqlDurations.Snapshot(DatabaseCommandCount, SqlFailureCount, InjectedDatabaseDelayMs);
    public void ResetSqlMetrics()
    {
        Interlocked.Exchange(ref _databaseCommandCount, 0);
        Interlocked.Exchange(ref _injectedDatabaseDelayMs, 0);
        Interlocked.Exchange(ref _sqlFailureCount, 0);
        _sqlDurations.Reset();
    }

    private readonly SqlDurationSamples _sqlDurations = new();
    public void RecordSqlDuration(TimeSpan elapsed) => _sqlDurations.Record(elapsed.TotalMilliseconds);
    public void RecordSqlFailure() => Interlocked.Increment(ref _sqlFailureCount);

    public async Task ApplyAsync(CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _databaseCommandCount);
        if (DatabaseFailure)
        {
            RecordSqlFailure();
            throw new InvalidOperationException("Injected load-test SQL failure.");
        }

        var delay = DatabaseDelayMs;
        if (delay > 0)
        {
            Interlocked.Add(ref _injectedDatabaseDelayMs, delay);
            await Task.Delay(delay, cancellationToken);
        }
    }

    public async Task ApplyAIAsync(CancellationToken cancellationToken)
    {
        if (AIUnavailable)
        {
            throw new HttpRequestException("Injected load-test AI outage.");
        }

        if (AIDelayMs > 0)
        {
            await Task.Delay(AIDelayMs, cancellationToken);
        }
    }
}

internal sealed class SqlDurationSamples
{
    private const int Capacity = 1_000_000;
    private readonly double[] _values = new double[Capacity];
    private int _count;
    public int Count => Math.Min(Volatile.Read(ref _count), Capacity);

    public void Record(double value)
    {
        var index = Interlocked.Increment(ref _count) - 1;
        if (index < Capacity) _values[index] = value;
    }

    public void Reset() => Interlocked.Exchange(ref _count, 0);

    public object Snapshot(long commandCount, long failures, long injectedDelayMs)
    {
        var values = _values.Take(Count).ToArray();
        Array.Sort(values);
        return new
        {
            commandCount,
            completedCommandCount = values.Length,
            failures,
            injectedDelayTotalMs = injectedDelayMs,
            p50Ms = Percentile(values, 0.50),
            p95Ms = Percentile(values, 0.95),
            p99Ms = Percentile(values, 0.99)
        };
    }

    private static double Percentile(double[] values, double percentile) => values.Length == 0
        ? 0
        : Math.Round(values[Math.Min(values.Length - 1, (int)Math.Ceiling(percentile * values.Length) - 1)], 2);
}

public sealed class LoadTestRunManifest
{
    public string RunId { get; init; } = ActivityTraceId.CreateRandom().ToString();
    public DateTime StartedAtUtc { get; init; } = DateTime.UtcNow;
    public string Environment { get; init; } = "LocalTestHost";
    public string BuildVersion { get; init; } = "unknown";
    public string DatasetProfile { get; init; } = "Small";
    public string Scenario { get; init; } = "mixed";
    public string WorkloadProfile { get; init; } = "Smoke";
    public int WarmupSeconds { get; init; }
    public int DurationSeconds { get; init; }

    public static async Task WriteAsync(string reportDirectory, object report, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(reportDirectory);
        var path = Path.Combine(reportDirectory, "run-summary.json");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(report,
            new JsonSerializerOptions { WriteIndented = true }), cancellationToken);
    }
}
