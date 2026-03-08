using NetworkSync.Core.Interfaces;
using NetworkSync.Core.Messages;
using NetworkSync.Core.Services;

namespace NetworkSync.Examples.SimpleServer;

/// <summary>
/// Simple example occupancy provider for demonstration.
/// In production, this would integrate with your core game state.
/// </summary>
public class ExampleOccupancyProvider : ICoreOccupancyProvider
{
    private readonly Dictionary<(int x, int y), CellOccupancyData> _occupancyMap;
    private readonly object _lock = new();

    public ExampleOccupancyProvider()
    {
        _occupancyMap = new Dictionary<(int, int), CellOccupancyData>();
    }

    public Task<bool> IsPlacementValidAsync(int x, int y, string structureType)
    {
        lock (_lock)
        {
            // Check bounds (example: 100x100 world)
            if (x < 0 || x >= 100 || y < 0 || y >= 100)
            {
                Console.WriteLine($"[Occupancy] Placement at ({x}, {y}) out of bounds");
                return Task.FromResult(false);
            }

            // Check if already occupied
            if (_occupancyMap.ContainsKey((x, y)))
            {
                Console.WriteLine($"[Occupancy] Placement at ({x}, {y}) already occupied");
                return Task.FromResult(false);
            }

            Console.WriteLine($"[Occupancy] Placement at ({x}, {y}) valid");
            return Task.FromResult(true);
        }
    }

    public Task ApplyPlacementAsync(int x, int y, string structureType, int rotation)
    {
        lock (_lock)
        {
            var structureId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _occupancyMap[(x, y)] = new CellOccupancyData
            {
                Occupied = true,
                EntityId = null,
                StructureId = structureId
            };

            Console.WriteLine($"[Occupancy] Applied {structureType} at ({x}, {y}), structure ID: {structureId}");
        }

        return Task.CompletedTask;
    }

    public Task<CellOccupancyData> GetCellOccupancyAsync(int x, int y)
    {
        lock (_lock)
        {
            var occupancy = _occupancyMap.GetValueOrDefault((x, y), new CellOccupancyData { Occupied = false });
            return Task.FromResult(occupancy);
        }
    }

    public Task<RegionOccupancyData> GetRegionOccupancyAsync(int x, int y, int width, int height)
    {
        lock (_lock)
        {
            var region = new RegionOccupancyData
            {
                Cells = new List<CellOccupancyData>()
            };

            for (int dy = 0; dy < height; dy++)
            {
                for (int dx = 0; dx < width; dx++)
                {
                    var cellX = x + dx;
                    var cellY = y + dy;
                    var occupancy = _occupancyMap.GetValueOrDefault((cellX, cellY), new CellOccupancyData { Occupied = false });
                    region.Cells.Add(occupancy);
                }
            }

            return Task.FromResult(region);
        }
    }

    /// <summary>
    /// Gets the current occupancy map for debugging.
    /// </summary>
    public Dictionary<(int x, int y), CellOccupancyData> GetOccupancyMap()
    {
        lock (_lock)
        {
            return new Dictionary<(int, int), CellOccupancyData>(_occupancyMap);
        }
    }
}

/// <summary>
/// Simple server example demonstrating NetworkSync usage.
/// </summary>
public class SimpleServerExample
{
    private readonly NetworkManager _networkManager;
    private readonly ExampleOccupancyProvider _occupancyProvider;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public SimpleServerExample()
    {
        _occupancyProvider = new ExampleOccupancyProvider();
        _networkManager = NetworkManager.CreateServer(_occupancyProvider, port: 7777, maxConnections: 10);
        _cancellationTokenSource = new CancellationTokenSource();

        // Subscribe to events
        _networkManager.PeerConnected += OnPeerConnected;
        _networkManager.PeerDisconnected += OnPeerDisconnected;
        _networkManager.Transport.MessageReceived += OnMessageReceived;
    }

    public async Task RunAsync()
    {
        Console.WriteLine("=== NetworkSync Simple Server Example ===");
        Console.WriteLine("Server starting on port 7777...");
        Console.WriteLine("Press Ctrl+C to stop");

        try
        {
            // Run until cancelled
            await Task.Delay(Timeout.Infinite, _cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("\nShutting down server...");
        }
        finally
        {
            await _networkManager.DisconnectAsync();
        }
    }

    private void OnPeerConnected(object? sender, PeerConnectedEventArgs e)
    {
        Console.WriteLine($"[Server] Client {e.PeerId} connected from {e.EndPoint}");

        // Send initial world snapshot to new client
        Task.Run(async () =>
        {
            await Task.Delay(100); // Small delay to ensure connection is stable
            await SendWorldSnapshotAsync(e.PeerId);
        });
    }

    private void OnPeerDisconnected(object? sender, PeerDisconnectedEventArgs e)
    {
        Console.WriteLine($"[Server] Client {e.PeerId} disconnected: {e.Reason}");
    }

    private void OnMessageReceived(object? sender, NetworkMessageEventArgs e)
    {
        switch (e.Message)
        {
            case PlacementCommandMessage command:
                Task.Run(() => HandlePlacementCommandAsync(e.PeerId, command));
                break;

            default:
                Console.WriteLine($"[Server] Received message type: {e.Message.MessageType}");
                break;
        }
    }

    private async Task HandlePlacementCommandAsync(int peerId, PlacementCommandMessage command)
    {
        Console.WriteLine($"[Server] Received placement command from client {peerId}: ({command.X}, {command.Y}) {command.StructureType}");

        // Process command
        var result = await _networkManager.ProcessPlacementCommandAsync(peerId, command);

        // Publish placement delta to all clients
        var delta = new PlacementDeltaMessage();
        _networkManager.ReplicationService.TrackPlacementChange(
            structureId: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            x: command.X,
            y: command.Y,
            type: PlacementDeltaMessage.ChangeType.Added,
            structureType: command.StructureType,
            rotation: command.Rotation
        );

        await _networkManager.ReplicationService.PublishPlacementDeltaAsync(delta);

        // Publish occupancy delta
        var occupancyDelta = new OccupancyDeltaMessage();
        _networkManager.ReplicationService.TrackOccupancyChange(
            x: command.X,
            y: command.Y,
            occupied: true,
            structureId: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        );

        await _networkManager.ReplicationService.PublishOccupancyDeltaAsync(occupancyDelta);

        Console.WriteLine($"[Server] Placement result: {result.Success}");
    }

    private async Task SendWorldSnapshotAsync(int peerId)
    {
        Console.WriteLine($"[Server] Sending world snapshot to client {peerId}");

        var snapshot = new WorldSnapshotMessage
        {
            TickNumber = 0,
            ServerTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        // Add all current placements to snapshot
        var occupancyMap = _occupancyProvider.GetOccupancyMap();
        foreach (var kvp in occupancyMap)
        {
            if (kvp.Value.StructureId.HasValue)
            {
                snapshot.Placements.Add(new WorldSnapshotMessage.PlacementSnapshotEntry
                {
                    X = kvp.Value.x,
                    Y = kvp.Value.y,
                    StructureType = "Structure",
                    Rotation = 0,
                    StructureId = kvp.Value.StructureId.Value
                });
            }

            snapshot.Occupancy.Add(new WorldSnapshotMessage.OccupancySnapshotEntry
            {
                X = kvp.Value.x,
                Y = kvp.Value.y,
                Occupied = kvp.Value.Occupied,
                EntityId = kvp.Value.EntityId,
                StructureId = kvp.Value.StructureId
            });
        }

        await _networkManager.Transport.SendAsync(peerId, snapshot, DeliveryMethod.ReliableOrdered);
        Console.WriteLine($"[Server] Sent snapshot with {snapshot.Placements.Count} placements and {snapshot.Occupancy.Count} occupancy entries");
    }

    public void Stop()
    {
        _cancellationTokenSource.Cancel();
    }
}

/// <summary>
/// Entry point for the simple server example.
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        var server = new SimpleServerExample();

        // Handle Ctrl+C
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            server.Stop();
        };

        await server.RunAsync();
    }
}
