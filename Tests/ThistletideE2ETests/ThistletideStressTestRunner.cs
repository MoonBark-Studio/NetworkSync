using System.Diagnostics;

namespace NetworkSync.Tests.ThistletideE2E;

/// <summary>
/// Metrics for Thistletide-specific stress tests.
/// </summary>
public class ThistletideTestMetrics
{
    private readonly Stopwatch _testStopwatch = new();
    
    // Connection metrics
    public int TotalConnections { get; private set; }
    public int SuccessfulConnections { get; private set; }
    public int FailedConnections { get; private set; }
    public long MinConnectionTimeMs { get; private set; } = long.MaxValue;
    public long MaxConnectionTimeMs { get; private set; }
    public long AvgConnectionTimeMs { get; private set; }
    private readonly List<long> _connectionTimes = new();

    // Message metrics
    public int TotalMessagesSent { get; private set; }
    public int TotalMessagesReceived { get; private set; }
    public long MinLatencyMs { get; private set; } = long.MaxValue;
    public long MaxLatencyMs { get; private set; }
    public long AvgLatencyMs { get; private set; }
    private readonly List<long> _latencies = new();

    // Placement metrics
    public int TotalPlacementCommands { get; private set; }
    public int SuccessfulPlacements { get; private set; }
    public int FailedPlacements { get; private set; }
    public int ServerPlacements { get; private set; }
    public int SyncMismatches { get; private set; }

    // Throughput
    public double MessagesPerSecond { get; private set; }
    public double PlacementsPerSecond { get; private set; }

    public TimeSpan ElapsedTime => _testStopwatch.Elapsed;

    public void Start() => _testStopwatch.Start();
    public void Stop() => _testStopwatch.Stop();

    public void RecordConnection(bool success, long timeMs)
    {
        TotalConnections++;
        if (success)
        {
            SuccessfulConnections++;
            _connectionTimes.Add(timeMs);
            if (timeMs < MinConnectionTimeMs) MinConnectionTimeMs = timeMs;
            if (timeMs > MaxConnectionTimeMs) MaxConnectionTimeMs = timeMs;
            AvgConnectionTimeMs = (long)_connectionTimes.Average();
        }
        else
        {
            FailedConnections++;
        }
    }

    public void RecordMessageSent() => TotalMessagesSent++;
    public void RecordMessageReceived(long latencyMs = 0)
    {
        TotalMessagesReceived++;
        if (latencyMs > 0)
        {
            _latencies.Add(latencyMs);
            if (latencyMs < MinLatencyMs) MinLatencyMs = latencyMs;
            if (latencyMs > MaxLatencyMs) MaxLatencyMs = latencyMs;
            AvgLatencyMs = (long)_latencies.Average();
        }
        MessagesPerSecond = TotalMessagesReceived / _testStopwatch.Elapsed.TotalSeconds;
    }

    public void RecordPlacement(bool success)
    {
        TotalPlacementCommands++;
        if (success)
        {
            SuccessfulPlacements++;
            PlacementsPerSecond = SuccessfulPlacements / _testStopwatch.Elapsed.TotalSeconds;
        }
        else
        {
            FailedPlacements++;
        }
    }

    public void RecordServerPlacement(int count) => ServerPlacements = count;
    public void RecordSyncMismatch() => SyncMismatches++;

    public void PrintSummary()
    {
        Console.WriteLine("\n" + new string('=', 60));
        Console.WriteLine("THISTLETIDE STRESS TEST RESULTS");
        Console.WriteLine(new string('=', 60));

        Console.WriteLine($"\n[Timing]");
        Console.WriteLine($"  Test Duration: {ElapsedTime:hh\\:mm\\:ss\\.fff}");

        Console.WriteLine($"\n[Connections]");
        Console.WriteLine($"  Total: {TotalConnections}");
        Console.WriteLine($"  Successful: {SuccessfulConnections}");
        Console.WriteLine($"  Failed: {FailedConnections}");
        Console.WriteLine($"  Connection Time (ms): min={MinConnectionTimeMs}, max={MaxConnectionTimeMs}, avg={AvgConnectionTimeMs}");

        Console.WriteLine($"\n[Messages]");
        Console.WriteLine($"  Total Sent: {TotalMessagesSent}");
        Console.WriteLine($"  Total Received: {TotalMessagesReceived}");
        Console.WriteLine($"  Latency (ms): min={MinLatencyMs}, max={MaxLatencyMs}, avg={AvgLatencyMs}");
        Console.WriteLine($"  Throughput: {MessagesPerSecond:F2} msg/sec");

        Console.WriteLine($"\n[Placements]");
        Console.WriteLine($"  Total Commands: {TotalPlacementCommands}");
        Console.WriteLine($"  Successful: {SuccessfulPlacements}");
        Console.WriteLine($"  Failed: {FailedPlacements}");
        Console.WriteLine($"  Server Placements: {ServerPlacements}");
        Console.WriteLine($"  Sync Mismatches: {SyncMismatches}");
        Console.WriteLine($"  Throughput: {PlacementsPerSecond:F2} placements/sec");

        Console.WriteLine(new string('=', 60));
    }
}

/// <summary>
/// Orchestrates Thistletide-specific stress tests with Thistletide occupancy map API.
/// </summary>
public class ThistletideStressTestRunner
{
    private readonly int _targetConnections;
    private readonly int _port;
    private readonly int _gridSize;
    private readonly ThistletideTestMetrics _metrics;

    public ThistletideStressTestRunner(int targetConnections, int port = 7777, int gridSize = 1000)
    {
        _targetConnections = targetConnections;
        _port = port;
        _gridSize = gridSize;
        _metrics = new ThistletideTestMetrics();
    }

    public async Task<ThistletideStressTestResult> RunTestAsync(
        int durationSeconds = 30,
        int batchSize = 10,
        int batchDelayMs = 100)
    {
        Console.WriteLine();
        Console.WriteLine("=".PadRight(60, '='));
        Console.WriteLine("THISTLETIDE STRESS TEST: " + _targetConnections + " connections");
        Console.WriteLine("Grid Size: " + _gridSize + "x" + _gridSize);
        Console.WriteLine("Duration: " + durationSeconds + "s");
        Console.WriteLine("=".PadRight(60, '='));

        var server = new ThistletideTestServer(_gridSize, _gridSize, _port, _targetConnections + 10);
        server.Start();
        
        // Wait longer for server to be ready
        await Task.Delay(2000);

        var clients = new List<ThistletideTestClient>();
        _metrics.Start();

        try
        {
            // Connect clients in batches
            Console.WriteLine();
            Console.WriteLine("[ThistletideTest] Connecting " + _targetConnections + " clients...");

            for (int i = 0; i < _targetConnections; i++)
            {
                var client = new ThistletideTestClient(_gridSize, _gridSize);
                clients.Add(client);

                var clientId = i + 1;
                var stopwatch = Stopwatch.StartNew();
                var connected = await client.ConnectAsync(clientId, "127.0.0.1", _port);
                stopwatch.Stop();

                _metrics.RecordConnection(connected, stopwatch.ElapsedMilliseconds);

                // Connect in batches
                if ((i + 1) % batchSize == 0 || i == _targetConnections - 1)
                {
                    await Task.Delay(batchDelayMs);
                    Console.WriteLine("[ThistletideTest] Connected: " + clients.Count(c => c.IsConnected) + "/" + _targetConnections);
                }
            }

            // Wait for stability
            await Task.Delay(1000);
            Console.WriteLine();
            Console.WriteLine("[ThistletideTest] Running for " + durationSeconds + " seconds...");

            // Send placement commands
            var random = new Random(42);
            var testEnd = DateTime.UtcNow.AddSeconds(durationSeconds);

            while (DateTime.UtcNow < testEnd)
            {
                foreach (var client in clients.Where(c => c.IsConnected).Take(_targetConnections / 2))
                {
                    var x = random.Next(0, _gridSize / 2);
                    var y = random.Next(0, _gridSize / 2);
                    var (success, latency) = await client.SendPlacementCommandAsync(x, y, "Wall");
                    _metrics.RecordMessageSent();
                    _metrics.RecordMessageReceived(latency);
                    _metrics.RecordPlacement(success);
                }

                await Task.Delay(10);
            }

            // Record final metrics
            _metrics.RecordServerPlacement(server.GetPlacementCount());

            // Verify sync
            var serverPlacements = server.GetAllPlacements();
            int mismatches = 0;
            foreach (var client in clients)
            {
                var clientPlacements = client.GetLocalPlacements();
                foreach (var kvp in serverPlacements)
                {
                    if (!clientPlacements.ContainsKey(kvp.Key))
                        mismatches++;
                }
            }
            _metrics.RecordSyncMismatch();

            _metrics.Stop();
            _metrics.PrintSummary();

            return new ThistletideStressTestResult
            {
                TargetConnections = _targetConnections,
                ActualConnections = clients.Count(c => c.IsConnected),
                Duration = _metrics.ElapsedTime,
                MessagesPerSecond = _metrics.MessagesPerSecond,
                PlacementsPerSecond = _metrics.PlacementsPerSecond,
                AvgConnectionTimeMs = _metrics.AvgConnectionTimeMs,
                AvgLatencyMs = _metrics.AvgLatencyMs,
                ServerPlacements = _metrics.ServerPlacements,
                SyncMismatches = _metrics.SyncMismatches,
                FailedConnections = _metrics.FailedConnections,
                Success = _metrics.FailedConnections == 0 && _metrics.SyncMismatches == 0
            };
        }
        finally
        {
            foreach (var client in clients)
            {
                try { await client.DisconnectAsync(); client.Dispose(); } catch { }
            }
            server.Stop();
            server.Dispose();
        }
    }
}

public class ThistletideStressTestResult
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

    public override string ToString()
    {
        return @"
            Thistletide Stress Test Result:
              Target Connections: " + TargetConnections + @"
              Actual Connections: " + ActualConnections + @"
              Duration: " + Duration.ToString(@"hh\:mm\:ss") + @"
              Messages/sec: " + MessagesPerSecond.ToString("F2") + @"
              Placements/sec: " + PlacementsPerSecond.ToString("F2") + @"
              Avg Connection Time: " + AvgConnectionTimeMs + @"ms
              Avg Latency: " + AvgLatencyMs + @"ms
              Server Placements: " + ServerPlacements + @"
              Sync Mismatches: " + SyncMismatches + @"
              Failed Connections: " + FailedConnections + @"
              Success: " + Success + @"
            ";
    }
}
