using System.Diagnostics;
using MoonBark.NetworkSync.Tests.ThistletideE2E.Simulations;

namespace MoonBark.NetworkSync.Tests.ThistletideE2E;

public sealed class ThistletideTestMetrics
{
    private readonly Stopwatch _testStopwatch = new();
    private readonly List<long> _connectionTimes = new();
    private readonly List<long> _latencies = new();

    public int TotalConnections { get; private set; }
    public int SuccessfulConnections { get; private set; }
    public int FailedConnections { get; private set; }
    public long MinConnectionTimeMs { get; private set; } = long.MaxValue;
    public long MaxConnectionTimeMs { get; private set; }
    public long AvgConnectionTimeMs { get; private set; }
    public int TotalMessagesSent { get; private set; }
    public int TotalMessagesReceived { get; private set; }
    public long MinLatencyMs { get; private set; } = long.MaxValue;
    public long MaxLatencyMs { get; private set; }
    public long AvgLatencyMs { get; private set; }
    public int TotalPlacementCommands { get; private set; }
    public int SuccessfulPlacements { get; private set; }
    public int FailedPlacements { get; private set; }
    public int ServerPlacements { get; private set; }
    public int SyncMismatches { get; private set; }
    public double MessagesPerSecond { get; private set; }
    public double PlacementsPerSecond { get; private set; }
    public TimeSpan ElapsedTime => _testStopwatch.Elapsed;

    public void Start() => _testStopwatch.Start();
    public void Stop() => _testStopwatch.Stop();

    public void RecordConnection(bool success, long timeMs)
    {
        TotalConnections++;
        if (!success)
        {
            FailedConnections++;
            return;
        }

        SuccessfulConnections++;
        _connectionTimes.Add(timeMs);
        MinConnectionTimeMs = Math.Min(MinConnectionTimeMs, timeMs);
        MaxConnectionTimeMs = Math.Max(MaxConnectionTimeMs, timeMs);
        AvgConnectionTimeMs = (long)_connectionTimes.Average();
    }

    public void RecordMessageSent() => TotalMessagesSent++;

    public void RecordMessageReceived(long latencyMs = 0)
    {
        TotalMessagesReceived++;
        if (latencyMs > 0)
        {
            _latencies.Add(latencyMs);
            MinLatencyMs = Math.Min(MinLatencyMs, latencyMs);
            MaxLatencyMs = Math.Max(MaxLatencyMs, latencyMs);
            AvgLatencyMs = (long)_latencies.Average();
        }

        if (_testStopwatch.Elapsed.TotalSeconds > 0)
        {
            MessagesPerSecond = TotalMessagesReceived / _testStopwatch.Elapsed.TotalSeconds;
        }
    }

    public void RecordPlacement(bool success)
    {
        TotalPlacementCommands++;
        if (!success)
        {
            FailedPlacements++;
            return;
        }

        SuccessfulPlacements++;
        if (_testStopwatch.Elapsed.TotalSeconds > 0)
        {
            PlacementsPerSecond = SuccessfulPlacements / _testStopwatch.Elapsed.TotalSeconds;
        }
    }

    public void RecordServerPlacement(int count) => ServerPlacements = count;
    public void RecordSyncMismatch() => SyncMismatches++;
}

public sealed class ThistletideStressTestRunner
{
    private readonly int _targetConnections;
    private readonly int _port;
    private readonly int _gridSize;

    public ThistletideStressTestRunner(int targetConnections, int port = 7777, int gridSize = 1000)
    {
        _targetConnections = targetConnections;
        _port = port;
        _gridSize = gridSize;
    }

    public async Task<ThistletideStressTestResult> RunTestAsync(int durationSeconds = 30, int batchSize = 10, int batchDelayMs = 100)
    {
        SimulationConfig config = new()
        {
            DurationSeconds = durationSeconds,
            GridWidth = _gridSize,
            GridHeight = _gridSize,
            Port = _port,
            MaxConnections = _targetConnections + 10,
            TargetConnections = _targetConnections,
            BatchSize = batchSize,
            BatchDelayMs = batchDelayMs
        };

        NetworkStressTestSimulation simulation = new(config);
        await simulation.InitializeAsync();
        simulation.Metrics.Start();

        try
        {
            await simulation.StartAsync();
        }
        finally
        {
            simulation.Metrics.Stop();
            await simulation.StopAsync();
            await simulation.DisposeAsync();
        }

        NetworkStressTestResult result = simulation.GetResult();

        return new ThistletideStressTestResult
        {
            TargetConnections = result.TargetConnections,
            ActualConnections = result.ActualConnections,
            Duration = result.Duration,
            MessagesPerSecond = result.MessagesPerSecond,
            PlacementsPerSecond = result.PlacementsPerSecond,
            AvgConnectionTimeMs = result.AvgConnectionTimeMs,
            AvgLatencyMs = result.AvgLatencyMs,
            ServerPlacements = result.ServerPlacements,
            SyncMismatches = result.SyncMismatches,
            FailedConnections = result.FailedConnections,
            Success = result.Success
        };
    }
}

public sealed class ThistletideStressTestResult
{
    public int TargetConnections { get; set; }
    public int ActualConnections { get; set; }
    public TimeSpan Duration { get; set; }
    public double MessagesPerSecond { get; set; }
    public double PlacementsPerSecond { get; set; }
    public long AvgConnectionTimeMs { get; set; }
    public long AvgLatencyMs { get; set; }
    public int ServerPlacements { get; set; }
    public int SyncMismatches { get; set; }
    public int FailedConnections { get; set; }
    public bool Success { get; set; }
}
