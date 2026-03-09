using System.Diagnostics;

namespace NetworkSync.Tests.ThistletideE2E.Metrics;

/// <summary>
/// Individual metric collector for a specific metric type.
/// </summary>
public class MetricCollector
{
    private readonly List<long> _values = new();
    private long _min = long.MaxValue;
    private long _max;
    private long _sum;

    public string Name { get; }
    public long Min => _min == long.MaxValue ? 0 : _min;
    public long Max => _max;
    public long Avg => _values.Count > 0 ? _sum / _values.Count : 0;
    public int Count => _values.Count;
    public long Sum => _sum;

    public MetricCollector(string name)
    {
        Name = name;
    }

    public void Record(long value)
    {
        _values.Add(value);
        _sum += value;
        if (value < _min) _min = value;
        if (value > _max) _max = value;
    }

    public void Reset()
    {
        _values.Clear();
        _min = long.MaxValue;
        _max = 0;
        _sum = 0;
    }
}

/// <summary>
/// Collects and manages test metrics.
/// </summary>
public class MetricsCollector
{
    private readonly Stopwatch _stopwatch = new();
    private readonly MetricCollector _connectionTime = new("Connection Time (ms)");
    private readonly MetricCollector _latency = new("Latency (ms)");
    
    // Counters
    private int _totalConnections;
    private int _successfulConnections;
    private int _failedConnections;
    private int _messagesSent;
    private int _messagesReceived;
    private int _placementCommands;
    private int _successfulPlacements;
    private int _failedPlacements;
    private int _serverPlacements;
    private int _syncMismatches;

    public TimeSpan ElapsedTime => _stopwatch.Elapsed;
    public double MessagesPerSecond => _stopwatch.Elapsed.TotalSeconds > 0 
        ? _messagesReceived / _stopwatch.Elapsed.TotalSeconds 
        : 0;
    public double PlacementsPerSecond => _stopwatch.Elapsed.TotalSeconds > 0
        ? _successfulPlacements / _stopwatch.Elapsed.TotalSeconds 
        : 0;

    public MetricCollector ConnectionTimeMetric => _connectionTime;
    public MetricCollector LatencyMetric => _latency;

    public int TotalConnections => _totalConnections;
    public int SuccessfulConnections => _successfulConnections;
    public int FailedConnections => _failedConnections;
    public int MessagesSent => _messagesSent;
    public int MessagesReceived => _messagesReceived;
    public int PlacementCommands => _placementCommands;
    public int SuccessfulPlacements => _successfulPlacements;
    public int FailedPlacements => _failedPlacements;
    public int ServerPlacements => _serverPlacements;
    public int SyncMismatches => _syncMismatches;

    public void Start() => _stopwatch.Start();
    public void Stop() => _stopwatch.Stop();
    public void Reset()
    {
        _stopwatch.Reset();
        _connectionTime.Reset();
        _latency.Reset();
        _totalConnections = 0;
        _successfulConnections = 0;
        _failedConnections = 0;
        _messagesSent = 0;
        _messagesReceived = 0;
        _placementCommands = 0;
        _successfulPlacements = 0;
        _failedPlacements = 0;
        _serverPlacements = 0;
        _syncMismatches = 0;
    }

    public void RecordConnection(bool success, long timeMs)
    {
        _totalConnections++;
        if (success)
        {
            _successfulConnections++;
            _connectionTime.Record(timeMs);
        }
        else
        {
            _failedConnections++;
        }
    }

    public void RecordMessageSent() => _messagesSent++;
    public void RecordMessageReceived(long latencyMs = 0)
    {
        _messagesReceived++;
        if (latencyMs > 0)
        {
            _latency.Record(latencyMs);
        }
    }

    public void RecordPlacement(bool success)
    {
        _placementCommands++;
        if (success)
        {
            _successfulPlacements++;
        }
        else
        {
            _failedPlacements++;
        }
    }

    public void RecordServerPlacements(int count) => _serverPlacements = count;
    public void RecordSyncMismatches(int count) => _syncMismatches = count;
}
