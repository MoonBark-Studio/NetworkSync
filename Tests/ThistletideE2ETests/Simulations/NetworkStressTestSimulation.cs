using System.Diagnostics;
using MoonBark.NetworkSync.Tests.ThistletideE2E.Components;
using MoonBark.NetworkSync.Tests.ThistletideE2E.Metrics;

namespace MoonBark.NetworkSync.Tests.ThistletideE2E.Simulations;

/// <summary>
/// Network stress test simulation using component-based architecture.
/// This simulation tests network performance with multiple concurrent clients.
/// </summary>
public class NetworkStressTestSimulation : SimulationScenario
{
    private readonly SimulationConfig _config;
    private ServerNodeComponent? _server;
    private ClientConnectionHandler? _clientHandler;
    private PlacementCommandGenerator? _commandGenerator;
    private readonly MetricsCollector _metrics = new();
    private CancellationTokenSource? _cancellationTokenSource;
    private PlacementCommandGenerator.PlacementPattern _placementPattern = PlacementCommandGenerator.PlacementPattern.Random;

    public NetworkStressTestSimulation(SimulationConfig? config = null)
    {
        _config = config ?? new SimulationConfig();
    }

    /// <summary>Gets the metrics collected during the simulation.</summary>
    public MetricsCollector Metrics => _metrics;

    /// <summary>Sets the placement pattern for the simulation.</summary>
    public void SetPlacementPattern(PlacementCommandGenerator.PlacementPattern pattern)
    {
        _placementPattern = pattern;
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        // Create server
        _server = new ServerNodeComponent(
            _config.GridWidth, 
            _config.GridHeight, 
            _config.Port, 
            _config.MaxConnections + 10
        );

        // Initialize command generator
        _commandGenerator = new PlacementCommandGenerator(
            _config.GridWidth, 
            _config.GridHeight, 
            _config.RandomSeed
        );

        // Initialize client handler
        _clientHandler = new ClientConnectionHandler(
            _config.GridWidth,
            _config.GridHeight,
            _config.BatchSize,
            _config.BatchDelayMs
        );

        _cancellationTokenSource = new CancellationTokenSource();
    }

    public override async Task StartAsync()
    {
        await base.StartAsync();

        if (_server == null || _clientHandler == null || _commandGenerator == null)
            throw new InvalidOperationException("Simulation not initialized");

        _server.Start();
        
        // Wait for server to be ready
        await Task.Delay(2000);

        // Connect clients
        await _clientHandler.ConnectClientsAsync(
            _config.TargetConnections, 
            "127.0.0.1", 
            _config.Port,
            (connected, total) => 
                Console.WriteLine($"[NetworkStressTest] Connected: {connected}/{total}")
        );

        // Wait for stability
        await Task.Delay(1000);

        // Run simulation
        await RunSimulationAsync();
    }

    private async Task RunSimulationAsync()
    {
        if (_clientHandler == null || _commandGenerator == null || _cancellationTokenSource == null)
            return;

        Console.WriteLine();
        Console.WriteLine($"[NetworkStressTest] Running for {_config.DurationSeconds} seconds...");

        var testEnd = DateTime.UtcNow.AddSeconds(_config.DurationSeconds);
        int iteration = 0;

        while (DateTime.UtcNow < testEnd && !_cancellationTokenSource.Token.IsCancellationRequested)
        {
            var activeClients = _clientHandler.GetActiveClients()
                .Take(_config.TargetConnections / 2)
                .ToList();

            foreach (var client in activeClients)
            {
                var (x, y, structureType) = _commandGenerator.GenerateNext(_placementPattern, iteration);
                var (success, latency) = await client.SendPlacementCommandAsync(x, y, structureType);
                
                _metrics.RecordMessageSent();
                _metrics.RecordMessageReceived(latency);
                _metrics.RecordPlacement(success);
            }

            iteration++;
            await Task.Delay(10);
        }
    }

    public override async Task StopAsync()
    {
        await base.StopAsync();

        // Record final metrics
        if (_server != null)
        {
            _metrics.RecordServerPlacements(_server.OccupancyMap.GetOccupiedCount());
        }

        // Verify sync
        if (_server != null && _clientHandler != null)
        {
            var serverPlacements = _server.OccupancyMap.GetAllOccupiedCells();
            int mismatches = 0;
            foreach (var client in _clientHandler.GetActiveClients())
            {
                var clientPlacements = client.LocalOccupancyMap.GetAllOccupiedCells();
                foreach (var kvp in serverPlacements)
                {
                    if (!clientPlacements.ContainsKey(kvp.Key))
                        mismatches++;
                }
            }
            _metrics.RecordSyncMismatches(mismatches);
        }
    }

    public override async Task DisposeAsync()
    {
        // Clean up clients
        if (_clientHandler != null)
        {
            await _clientHandler.DisconnectAllAsync();
        }

        // Clean up server
        if (_server != null)
        {
            _server.Stop();
            _server.Dispose();
            _server = null;
        }

        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();

        await base.DisposeAsync();
    }

    /// <summary>
    /// Gets the result of the simulation.
    /// </summary>
    public NetworkStressTestResult GetResult()
    {
        return new NetworkStressTestResult
        {
            TargetConnections = _config.TargetConnections,
            ActualConnections = _clientHandler?.ConnectedCount ?? 0,
            Duration = _metrics.ElapsedTime,
            MessagesPerSecond = _metrics.MessagesPerSecond,
            PlacementsPerSecond = _metrics.PlacementsPerSecond,
            AvgConnectionTimeMs = _metrics.ConnectionTimeMetric.Avg,
            AvgLatencyMs = _metrics.LatencyMetric.Avg,
            ServerPlacements = _metrics.ServerPlacements,
            SyncMismatches = _metrics.SyncMismatches,
            FailedConnections = _metrics.FailedConnections,
            Success = _metrics.FailedConnections == 0 && _metrics.SyncMismatches == 0
        };
    }
}

/// <summary>
/// Result of a network stress test.
/// </summary>
public class NetworkStressTestResult
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
        return $@"
            Network Stress Test Result:
              Target Connections: {TargetConnections}
              Actual Connections: {ActualConnections}
              Duration: {Duration:hh\:mm\:ss}
              Messages/sec: {MessagesPerSecond:F2}
              Placements/sec: {PlacementsPerSecond:F2}
              Avg Connection Time: {AvgConnectionTimeMs}ms
              Avg Latency: {AvgLatencyMs}ms
              Server Placements: {ServerPlacements}
              Sync Mismatches: {SyncMismatches}
              Failed Connections: {FailedConnections}
              Success: {Success}
            ";
    }
}
