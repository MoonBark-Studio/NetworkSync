using System.Diagnostics;

namespace MoonBark.NetworkSync.Tests.StressTests.Infrastructure;

/// <summary>
/// Orchestrates stress tests with multiple concurrent clients.
/// </summary>
public class StressTestRunner
{
    private readonly int _targetConnections;
    private readonly int _port;
    private readonly TestMetrics _metrics;

    public TestMetrics Metrics => _metrics;

    public StressTestRunner(int targetConnections, int port = 7777)
    {
        _targetConnections = targetConnections;
        _port = port;
        _metrics = new TestMetrics();
    }

    /// <summary>
    /// Runs a stress test with the specified number of concurrent connections.
    /// </summary>
    /// <param name="durationSeconds">Duration of the test in seconds</param>
    /// <param name="placementsPerClient">Number of placement commands each client should attempt</param>
    /// <param name="batchSize">Number of clients to connect per batch</param>
    /// <param name="batchDelayMs">Delay between connection batches</param>
    public async Task<StressTestResult> RunTestAsync(
        int durationSeconds = 30,
        int placementsPerClient = 10,
        int batchSize = 10,
        int batchDelayMs = 100)
    {
        Console.WriteLine("\n" + new string('=', 60));
        Console.WriteLine($"STARTING STRESS TEST: {_targetConnections} connections");
        Console.WriteLine($"Duration: {durationSeconds}s, Placements/Client: {placementsPerClient}");
        Console.WriteLine($"Batch Size: {batchSize}, Batch Delay: {batchDelayMs}ms");
        Console.WriteLine(new string('=', 60));

        var server = new StressTestServer(_port, _targetConnections + 10);
        server.Start();

        // Give server time to start
        await Task.Delay(500);

        var clients = new List<StressTestClient>();
        var connectionTasks = new List<Task<bool>>();

        try
        {
            // Connect clients in batches
            Console.WriteLine($"\n[StressTest] Connecting {_targetConnections} clients in batches of {batchSize}...");

            for (int i = 0; i < _targetConnections; i++)
            {
                var client = new StressTestClient("127.0.0.1", _port);
                clients.Add(client);

                var clientId = i + 1;
                var connectTask = ConnectClientAsync(client, clientId);
                connectionTasks.Add(connectTask);

                // Connect in batches
                if ((i + 1) % batchSize == 0 || i == _targetConnections - 1)
                {
                    await Task.Delay(batchDelayMs);

                    // Wait for batch to complete
                    var batchResults = await Task.WhenAll(connectionTasks);
                    var successful = batchResults.Count(r => r);
                    Console.WriteLine($"[StressTest] Batch complete: {successful}/{connectionTasks.Count} connected");

                    connectionTasks.Clear();
                }
            }

            // Wait for all connections to stabilize
            await Task.Delay(1000);

            Console.WriteLine($"\n[StressTest] Running test for {durationSeconds} seconds...");
            Console.WriteLine($"[StressTest] Connected clients: {clients.Count(c => c.IsConnected)}");

            // Run placement commands
            var placementTasks = new List<Task>();
            var random = new Random(42); // Fixed seed for reproducibility

            var testStopwatch = Stopwatch.StartNew();
            var endTime = DateTime.UtcNow.AddSeconds(durationSeconds);

            while (DateTime.UtcNow < endTime)
            {
                // Send placement commands from active clients
                foreach (var client in clients.Where(c => c.IsConnected).Take(_targetConnections / 2))
                {
                    var x = random.Next(0, 500);
                    var y = random.Next(0, 500);
                    var task = client.SendPlacementCommandAsync(x, y, "TestStructure");
                    placementTasks.Add(task);
                    _metrics.RecordMessageSent();
                }

                // Limit concurrent operations
                if (placementTasks.Count > 1000)
                {
                    await Task.WhenAll(placementTasks.Take(500));
                    placementTasks.RemoveRange(0, 500);
                }

                await Task.Delay(10);
            }

            // Wait for pending operations
            if (placementTasks.Count > 0)
            {
                await Task.WhenAll(placementTasks);
            }

            testStopwatch.Stop();

            // Record final metrics
            _metrics.RecordServerPlacement(server.GetPlacementCount());
            var clientPlacements = clients.Sum(c => c.GetLocalPlacementCount());
            _metrics.RecordClientPlacement(clientPlacements);

            // Check for sync mismatches
            var serverPlacements = server.GetAllPlacements();
            int syncMismatches = 0;
            foreach (var client in clients)
            {
                var clientPlacements2 = client.GetLocalPlacements();
                foreach (var kvp in clientPlacements2)
                {
                    if (!serverPlacements.ContainsKey(kvp.Key))
                    {
                        syncMismatches++;
                    }
                }
            }
            _metrics.RecordSyncMismatch();

            _metrics.Stop();

            // Print summary
            _metrics.PrintSummary();

            // Generate result
            var snapshot = _metrics.GetSnapshot();
            return new StressTestResult
            {
                TargetConnections = _targetConnections,
                ActualConnections = clients.Count(c => c.IsConnected),
                Duration = snapshot.ElapsedTime,
                MessagesPerSecond = snapshot.MessagesPerSecond,
                PlacementsPerSecond = snapshot.PlacementsPerSecond,
                AvgConnectionTimeMs = snapshot.AvgConnectionTimeMs,
                AvgMessageLatencyMs = snapshot.AvgMessageLatencyMs,
                SyncMismatches = snapshot.SyncMismatches,
                FailedConnections = snapshot.FailedConnections,
                Success = snapshot.FailedConnections == 0 && snapshot.SyncMismatches == 0
            };
        }
        finally
        {
            // Cleanup
            Console.WriteLine("\n[StressTest] Disconnecting clients...");
            foreach (var client in clients)
            {
                try
                {
                    await client.DisconnectAsync();
                    client.Dispose();
                }
                catch { }
            }

            server.Stop();
            server.Dispose();
        }
    }

    private async Task<bool> ConnectClientAsync(StressTestClient client, int clientId)
    {
        var stopwatch = Stopwatch.StartNew();
        var connected = await client.ConnectAsync(clientId);
        stopwatch.Stop();

        _metrics.RecordConnection(connected, stopwatch.ElapsedMilliseconds);

        return connected;
    }
}

public class StressTestResult
{
    public int TargetConnections { get; set; }
    public int ActualConnections { get; set; }
    public TimeSpan Duration { get; set; }
    public double MessagesPerSecond { get; set; }
    public double PlacementsPerSecond { get; set; }
    public long AvgConnectionTimeMs { get; set; }
    public long AvgMessageLatencyMs { get; set; }
    public int SyncMismatches { get; set; }
    public int FailedConnections { get; set; }
    public bool Success { get; set; }

    public override string ToString()
    {
        return $"""
            Stress Test Result:
              Target Connections: {TargetConnections}
              Actual Connections: {ActualConnections}
              Duration: {Duration:hh\\:mm\\:ss}
              Messages/sec: {MessagesPerSecond:F2}
              Placements/sec: {PlacementsPerSecond:F2}
              Avg Connection Time: {AvgConnectionTimeMs}ms
              Avg Message Latency: {AvgMessageLatencyMs}ms
              Sync Mismatches: {SyncMismatches}
              Failed Connections: {FailedConnections}
              Success: {Success}
            """;
    }
}
