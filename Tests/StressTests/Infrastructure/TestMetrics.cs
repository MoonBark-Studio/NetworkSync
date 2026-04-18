using System.Diagnostics;

namespace MoonBark.NetworkSync.Tests.StressTests.Infrastructure;

/// <summary>
/// Collects and tracks metrics during stress tests.
/// </summary>
public class TestMetrics
{
    private readonly Stopwatch _testStopwatch;
    private readonly List<ConnectionMetrics> _connectionMetrics;
    private readonly List<MessageMetrics> _messageMetrics;
    private readonly object _lock = new();

    // Connection metrics
    public int TotalConnections { get; private set; }
    public int SuccessfulConnections { get; private set; }
    public int FailedConnections { get; private set; }
    public int TotalDisconnections { get; private set; }

    // Message metrics
    public int TotalMessagesSent { get; private set; }
    public int TotalMessagesReceived { get; private set; }
    public int TotalPlacementCommands { get; private set; }
    public int SuccessfulPlacements { get; private set; }
    public int FailedPlacements { get; private set; }

    // Timing metrics
    public long MinConnectionTimeMs { get; private set; } = long.MaxValue;
    public long MaxConnectionTimeMs { get; private set; }
    public long AvgConnectionTimeMs { get; private set; }
    public long MinMessageLatencyMs { get; private set; } = long.MaxValue;
    public long MaxMessageLatencyMs { get; private set; }
    public long AvgMessageLatencyMs { get; private set; }

    // Data sync metrics
    public int ServerPlacements { get; private set; }
    public int ClientPlacements { get; private set; }
    public int SyncMismatches { get; private set; }

    // Throughput
    public double MessagesPerSecond { get; private set; }
    public double PlacementsPerSecond { get; private set; }

    public TimeSpan ElapsedTime => _testStopwatch.Elapsed;

    public TestMetrics()
    {
        _testStopwatch = Stopwatch.StartNew();
        _connectionMetrics = new List<ConnectionMetrics>();
        _messageMetrics = new List<MessageMetrics>();
    }

    public void RecordConnection(bool success, long connectionTimeMs)
    {
        lock (_lock)
        {
            TotalConnections++;
            if (success)
            {
                SuccessfulConnections++;
                _connectionMetrics.Add(new ConnectionMetrics
                {
                    ConnectionTimeMs = connectionTimeMs,
                    ConnectedAt = DateTime.UtcNow
                });

                if (connectionTimeMs < MinConnectionTimeMs) MinConnectionTimeMs = connectionTimeMs;
                if (connectionTimeMs > MaxConnectionTimeMs) MaxConnectionTimeMs = connectionTimeMs;

                AvgConnectionTimeMs = (long)_connectionMetrics.Average(c => c.ConnectionTimeMs);
            }
            else
            {
                FailedConnections++;
            }
        }
    }

    public void RecordDisconnection()
    {
        lock (_lock)
        {
            TotalDisconnections++;
        }
    }

    public void RecordMessageSent()
    {
        lock (_lock)
        {
            TotalMessagesSent++;
            MessagesPerSecond = TotalMessagesSent / _testStopwatch.Elapsed.TotalSeconds;
        }
    }

    public void RecordMessageReceived(long latencyMs = 0)
    {
        lock (_lock)
        {
            TotalMessagesReceived++;

            if (latencyMs > 0)
            {
                _messageMetrics.Add(new MessageMetrics
                {
                    LatencyMs = latencyMs,
                    ReceivedAt = DateTime.UtcNow
                });

                if (latencyMs < MinMessageLatencyMs) MinMessageLatencyMs = latencyMs;
                if (latencyMs > MaxMessageLatencyMs) MaxMessageLatencyMs = latencyMs;
                AvgMessageLatencyMs = (long)_messageMetrics.Average(m => m.LatencyMs);
            }
        }
    }

    public void RecordPlacementCommand(bool success)
    {
        lock (_lock)
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
    }

    public void RecordServerPlacement(int count)
    {
        lock (_lock)
        {
            ServerPlacements = count;
        }
    }

    public void RecordClientPlacement(int count)
    {
        lock (_lock)
        {
            ClientPlacements = count;
        }
    }

    public void RecordSyncMismatch()
    {
        lock (_lock)
        {
            SyncMismatches++;
        }
    }

    public void Stop()
    {
        _testStopwatch.Stop();
    }

    public TestMetricsSnapshot GetSnapshot()
    {
        lock (_lock)
        {
            return new TestMetricsSnapshot
            {
                ElapsedTime = _testStopwatch.Elapsed,
                TotalConnections = TotalConnections,
                SuccessfulConnections = SuccessfulConnections,
                FailedConnections = FailedConnections,
                TotalDisconnections = TotalDisconnections,
                TotalMessagesSent = TotalMessagesSent,
                TotalMessagesReceived = TotalMessagesReceived,
                TotalPlacementCommands = TotalPlacementCommands,
                SuccessfulPlacements = SuccessfulPlacements,
                FailedPlacements = FailedPlacements,
                MinConnectionTimeMs = MinConnectionTimeMs == long.MaxValue ? 0 : MinConnectionTimeMs,
                MaxConnectionTimeMs = MaxConnectionTimeMs,
                AvgConnectionTimeMs = AvgConnectionTimeMs,
                MinMessageLatencyMs = MinMessageLatencyMs == long.MaxValue ? 0 : MinMessageLatencyMs,
                MaxMessageLatencyMs = MaxMessageLatencyMs,
                AvgMessageLatencyMs = AvgMessageLatencyMs,
                ServerPlacements = ServerPlacements,
                ClientPlacements = ClientPlacements,
                SyncMismatches = SyncMismatches,
                MessagesPerSecond = MessagesPerSecond,
                PlacementsPerSecond = PlacementsPerSecond
            };
        }
    }

    public void PrintSummary()
    {
        var snapshot = GetSnapshot();

        Console.WriteLine("\n" + new string('=', 60));
        Console.WriteLine("STRESS TEST METRICS SUMMARY");
        Console.WriteLine(new string('=', 60));

        Console.WriteLine($"\n[Timing]");
        Console.WriteLine($"  Test Duration: {snapshot.ElapsedTime:hh\\:mm\\:ss\\.fff}");

        Console.WriteLine($"\n[Connections]");
        Console.WriteLine($"  Total Attempts: {snapshot.TotalConnections}");
        Console.WriteLine($"  Successful: {snapshot.SuccessfulConnections}");
        Console.WriteLine($"  Failed: {snapshot.FailedConnections}");
        Console.WriteLine($"  Disconnected: {snapshot.TotalDisconnections}");
        Console.WriteLine($"  Connection Time (ms): min={snapshot.MinConnectionTimeMs}, max={snapshot.MaxConnectionTimeMs}, avg={snapshot.AvgConnectionTimeMs}");

        Console.WriteLine($"\n[Messages]");
        Console.WriteLine($"  Total Sent: {snapshot.TotalMessagesSent}");
        Console.WriteLine($"  Total Received: {snapshot.TotalMessagesReceived}");
        Console.WriteLine($"  Latency (ms): min={snapshot.MinMessageLatencyMs}, max={snapshot.MaxMessageLatencyMs}, avg={snapshot.AvgMessageLatencyMs}");
        Console.WriteLine($"  Throughput: {snapshot.MessagesPerSecond:F2} msg/sec");

        Console.WriteLine($"\n[Placement Commands]");
        Console.WriteLine($"  Total Commands: {snapshot.TotalPlacementCommands}");
        Console.WriteLine($"  Successful: {snapshot.SuccessfulPlacements}");
        Console.WriteLine($"  Failed: {snapshot.FailedPlacements}");
        Console.WriteLine($"  Throughput: {snapshot.PlacementsPerSecond:F2} placements/sec");

        Console.WriteLine($"\n[Data Sync]");
        Console.WriteLine($"  Server Placements: {snapshot.ServerPlacements}");
        Console.WriteLine($"  Client Placements: {snapshot.ClientPlacements}");
        Console.WriteLine($"  Sync Mismatches: {snapshot.SyncMismatches}");

        Console.WriteLine(new string('=', 60));
    }
}

public class ConnectionMetrics
{
    public long ConnectionTimeMs { get; set; }
    public DateTime ConnectedAt { get; set; }
}

public class MessageMetrics
{
    public long LatencyMs { get; set; }
    public DateTime ReceivedAt { get; set; }
}

public class TestMetricsSnapshot
{
    public TimeSpan ElapsedTime { get; set; }

    // Connections
    public int TotalConnections { get; set; }
    public int SuccessfulConnections { get; set; }
    public int FailedConnections { get; set; }
    public int TotalDisconnections { get; set; }

    // Messages
    public int TotalMessagesSent { get; set; }
    public int TotalMessagesReceived { get; set; }

    // Placements
    public int TotalPlacementCommands { get; set; }
    public int SuccessfulPlacements { get; set; }
    public int FailedPlacements { get; set; }

    // Timing
    public long MinConnectionTimeMs { get; set; }
    public long MaxConnectionTimeMs { get; set; }
    public long AvgConnectionTimeMs { get; set; }
    public long MinMessageLatencyMs { get; set; }
    public long MaxMessageLatencyMs { get; set; }
    public long AvgMessageLatencyMs { get; set; }

    // Data Sync
    public int ServerPlacements { get; set; }
    public int ClientPlacements { get; set; }
    public int SyncMismatches { get; set; }

    // Throughput
    public double MessagesPerSecond { get; set; }
    public double PlacementsPerSecond { get; set; }
}
