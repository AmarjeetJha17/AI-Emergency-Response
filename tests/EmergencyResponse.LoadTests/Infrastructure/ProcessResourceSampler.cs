using System.Diagnostics;

namespace EmergencyResponse.LoadTests.Infrastructure;

public sealed class ProcessResourceSampler : IAsyncDisposable
{
    private readonly Process _process = Process.GetCurrentProcess();
    private readonly CancellationTokenSource _cancellation = new();
    private readonly Task _samplingTask;
    private long _peakWorkingSet;
    private long _peakManagedHeap;
    private long _peakLargeObjectHeap;
    private long _cpuPercentThousandths;
    private long _sampleCount;
    private readonly long _allocatedBefore = GC.GetTotalAllocatedBytes(precise: false);
    private readonly int _gen0Before = GC.CollectionCount(0);
    private readonly int _gen1Before = GC.CollectionCount(1);
    private readonly int _gen2Before = GC.CollectionCount(2);

    private ProcessResourceSampler()
    {
        UpdatePeak(ref _peakWorkingSet, _process.WorkingSet64);
        UpdatePeak(ref _peakManagedHeap, GC.GetTotalMemory(forceFullCollection: false));
        UpdatePeak(ref _peakLargeObjectHeap, GetLargeObjectHeapSizeBytes());
        _samplingTask = Task.Run(SampleAsync);
    }

    public static ProcessResourceSampler Start() => new();

    public async Task StopAsync()
    {
        _cancellation.Cancel();
        try
        {
            await _samplingTask;
        }
        catch (OperationCanceledException)
        {
            // Expected when the sampler is stopped after NBomber completes.
        }
    }

    public object Snapshot()
    {
        var samples = Interlocked.Read(ref _sampleCount);
        return new
        {
            samples,
            peakWorkingSetBytes = Interlocked.Read(ref _peakWorkingSet),
            peakManagedHeapBytes = Interlocked.Read(ref _peakManagedHeap),
            peakLargeObjectHeapBytes = Interlocked.Read(ref _peakLargeObjectHeap),
            allocatedBytes = GC.GetTotalAllocatedBytes(precise: false) - _allocatedBefore,
            gen0Collections = GC.CollectionCount(0) - _gen0Before,
            gen1Collections = GC.CollectionCount(1) - _gen1Before,
            gen2Collections = GC.CollectionCount(2) - _gen2Before,
            averageProcessCpuPercent = samples == 0 ? 0 : Math.Round((double)Interlocked.Read(ref _cpuPercentThousandths) / samples / 1000, 2),
            processorCount = Environment.ProcessorCount
        };
    }

    private async Task SampleAsync()
    {
        var previousCpu = _process.TotalProcessorTime;
        var previousTimestamp = Stopwatch.GetTimestamp();
        while (true)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), _cancellation.Token);
            _process.Refresh();
            UpdatePeak(ref _peakWorkingSet, _process.WorkingSet64);
            UpdatePeak(ref _peakManagedHeap, GC.GetTotalMemory(forceFullCollection: false));
            UpdatePeak(ref _peakLargeObjectHeap, GetLargeObjectHeapSizeBytes());

            var timestamp = Stopwatch.GetTimestamp();
            var cpu = _process.TotalProcessorTime;
            var elapsedSeconds = Stopwatch.GetElapsedTime(previousTimestamp, timestamp).TotalSeconds;
            var cpuPercent = elapsedSeconds <= 0 ? 0 :
                (cpu - previousCpu).TotalSeconds / elapsedSeconds / Math.Max(1, Environment.ProcessorCount) * 100;
            Interlocked.Add(ref _cpuPercentThousandths, (long)(Math.Clamp(cpuPercent, 0, 100) * 1000));
            Interlocked.Increment(ref _sampleCount);
            previousCpu = cpu;
            previousTimestamp = timestamp;
        }
    }

    private static void UpdatePeak(ref long peak, long value)
    {
        var current = Interlocked.Read(ref peak);
        while (value > current)
        {
            var previous = Interlocked.CompareExchange(ref peak, value, current);
            if (previous == current) return;
            current = previous;
        }
    }

    private static long GetLargeObjectHeapSizeBytes()
    {
        var generations = GC.GetGCMemoryInfo().GenerationInfo;
        return generations.Length > 3 ? generations[3].SizeAfterBytes : 0;
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _cancellation.Dispose();
        _process.Dispose();
    }
}
