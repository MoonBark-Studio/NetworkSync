using MoonBark.NetworkSync.Core.Interfaces;
using MoonBark.NetworkSync.Core.Messages;
using MoonBark.NetworkSync.Core.Services;

namespace MoonBark.NetworkSync.Examples.SimpleClient;

/// <summary>
/// Simple example local validator for demonstration.
/// In production, this would integrate with your client-side game state.
/// </summary>
public class ExampleLocalValidator : ILocalOccupancyValidator
{
    private readonly Dictionary<(int x, int y), MoonBark.NetworkSync.Core.Messages.CellOccupancyData> _localOccupancy;
    private readonly object _lock = new();

    public ExampleLocalValidator()
    {
        _localOccupancy = new Dictionary<(int, int), MoonBark.NetworkSync.Core.Messages.CellOccupancyData>();
    }

    public bool IsPlacementValidLocally(int x, int y, string structureType)
    {
        lock (_lock)
        {
            // Check bounds (example: 100x100 world)
            if (x < 0 || x >= 100 || y < 0 || y >= 100)
            {
                Console.WriteLine($"[LocalValidator] Placement at ({x}, {y}) out of bounds");
                return false;
            }

            // Check if already occupied locally
            if (_localOccupancy.ContainsKey((x, y)))
            {
                Console.WriteLine($"[LocalValidator] Placement at ({x}, {y}) already occupied");
                return false;
            }

            Console.WriteLine($"[LocalValidator] Placement at ({x}, {y}) valid locally");
            return true;
        }
    }

    public void UpdateLocalOccupancy(int x, int y, bool occupied, long? entityId = null, long? structureId = null)
    {
        lock (_lock)
        {
            if (occupied)
            {
                _localOccupancy[(x, y)] = new MoonBark.NetworkSync.Core.Messages.CellOccupancyData
                {
                    Occupied = true,
                    EntityId = entityId,
                    StructureId = structureId
                };
                Console.WriteLine($"[LocalValidator] Updated occupancy at ({x}, {y}): occupied={occupied}");
            }
            else
            {
                _localOccupancy.Remove((x, y));
                Console.WriteLine($"[LocalValidator] Cleared occupancy at ({x}, {y})");
            }
        }
    }

    /// <summary>
    /// Gets the current local occupancy map for debugging.
    /// </summary>
    public Dictionary<(int x, int y), MoonBark.NetworkSync.Core.Messages.CellOccupancyData> GetLocalOccupancy()
    {
        lock (_lock)
        {
            return new Dictionary<(int, int), MoonBark.NetworkSync.Core.Messages.CellOccupancyData>(_localOccupancy);
        }
    }
}

/// <summary>
/// Simple client example demonstrating NetworkSync usage.
/// </summary>
public class SimpleClientExample
{
    private readonly NetworkManager _networkManager;
    private readonly ExampleLocalValidator _localValidator;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private long _nextCommandId;

    public SimpleClientExample()
    {
        _localValidator = new ExampleLocalValidator();
        _networkManager = NetworkManager.CreateClient(_localValidator);
        _cancellationTokenSource = new CancellationTokenSource();
        _nextCommandId = 1;

        // Subscribe to events
        _networkManager.Transport.MessageReceived += OnMessageReceived;
        _networkManager.ReplicationService.PlacementDeltaReceived += OnPlacementDeltaReceived;
        _networkManager.ReplicationService.OccupancyDeltaReceived += OnOccupancyDeltaReceived;
        _networkManager.ReplicationService.WorldSnapshotReceived += OnWorldSnapshotReceived;
        _networkManager.ClientPrediction.PredictionMismatch += OnPredictionMismatch;
    }

    public async Task RunAsync(string host = "127.0.0.1", int port = 7777)
    {
        Console.WriteLine("=== NetworkSync Simple Client Example ===");
        Console.WriteLine($"Connecting to {host}:{port}...");

        try
        {
            await _networkManager.ConnectAsync(host, port);
            Console.WriteLine("Connected to server!");

            // Start interactive command loop
            await RunCommandLoopAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            await _networkManager.DisconnectAsync();
        }
    }

    private async Task RunCommandLoopAsync()
    {
        Console.WriteLine("\nCommands:");
        Console.WriteLine("  place <x> <y> <type>  - Place a structure");
        Console.WriteLine("  status                 - Show current status");
        Console.WriteLine("  quit                   - Exit");

        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            Console.Write("\n> ");
            var input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                continue;
            }

            var parts = input.Split(' ');
            var command = parts[0].ToLower();

            switch (command)
            {
                case "place":
                    if (parts.Length >= 4)
                    {
                        var x = int.Parse(parts[1]);
                        var y = int.Parse(parts[2]);
                        var type = parts[3];
                        await PlaceStructureAsync(x, y, type);
                    }
                    else
                    {
                        Console.WriteLine("Usage: place <x> <y> <type>");
                    }
                    break;

                case "status":
                    ShowStatus();
                    break;

                case "quit":
                    _cancellationTokenSource.Cancel();
                    break;

                default:
                    Console.WriteLine($"Unknown command: {command}");
                    break;
            }
        }
    }

    private async Task PlaceStructureAsync(int x, int y, string structureType)
    {
        var command = new PlacementCommandMessage
        {
            CommandId = _nextCommandId++,
            ClientId = 1, // In a real client, this would be assigned by server
            X = x,
            Y = y,
            StructureType = structureType,
            Rotation = 0,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        Console.WriteLine($"[Client] Sending placement command: ({x}, {y}) {structureType}");

        // Send command (this predicts locally and sends to server)
        var predictedResult = await _networkManager.SendPlacementCommandAsync(command);

        // Show predicted result immediately
        if (predictedResult.Success)
        {
            Console.WriteLine($"[Client] ✅ Prediction: Placement valid at ({x}, {y})");
        }
        else
        {
            Console.WriteLine($"[Client] ❌ Prediction: {predictedResult.FailureReason}");
        }
    }

    private void ShowStatus()
    {
        Console.WriteLine("\n=== Client Status ===");
        Console.WriteLine($"Connected: {_networkManager.IsConnected}");
        Console.WriteLine($"Server: {_networkManager.IsServer}");
        Console.WriteLine($"Pending Commands: {_networkManager.ClientPrediction?.GetPredictedPlacements()?.Count() ?? 0}");

        var localOccupancy = _localValidator.GetLocalOccupancy();
        Console.WriteLine($"Local Occupancy: {localOccupancy.Count} cells");
    }

    private void OnMessageReceived(object? sender, NetworkMessageEventArgs e)
    {
        switch (e.Message)
        {
            case PlacementResultMessage result:
                HandlePlacementResult(result);
                break;

            default:
                Console.WriteLine($"[Client] Received message type: {e.Message.MessageType}");
                break;
        }
    }

    private void HandlePlacementResult(PlacementResultMessage result)
    {
        Console.WriteLine($"[Client] Received placement result for command {result.CommandId}: {result.Success}");

        // Get pending command result
        var pendingResult = _networkManager.ClientPrediction.GetPendingCommandResult(result.CommandId);
        if (pendingResult != null)
        {
            // Reconcile prediction
            _networkManager.ClientPrediction.ReconcilePlacement(result, pendingResult);

            // Mark command as completed
            _networkManager.ClientPrediction.MarkCommandCompleted(result.CommandId);

            // Update local validator with authoritative result
            if (result.Success)
            {
                // In a real implementation, we'd need to track the placement details
                // For now, this is a placeholder
                Console.WriteLine($"[Client] ✅ Server confirmed placement");
            }
            else
            {
                Console.WriteLine($"[Client] ❌ Server rejected placement: {result.FailureReason}");
            }
        }
    }

    private void OnPlacementDeltaReceived(object? sender, PlacementDeltaReceivedEventArgs e)
    {
        Console.WriteLine($"[Client] Received placement delta (tick {e.Delta.TickNumber}) with {e.Delta.Changes.Count} changes");

        foreach (var change in e.Delta.Changes)
        {
            Console.WriteLine($"[Client]   Placement change: ({change.X}, {change.Y}) {change.Type} {change.StructureType}");

            // Update local validator
            if (change.Type == PlacementDeltaChangeType.Added)
            {
                _localValidator.UpdateLocalOccupancy(change.X, change.Y, true, structureId: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            }
            else if (change.Type == PlacementDeltaChangeType.Removed)
            {
                _localValidator.UpdateLocalOccupancy(change.X, change.Y, false);
            }
        }
    }

    private void OnOccupancyDeltaReceived(object? sender, OccupancyDeltaReceivedEventArgs e)
    {
        Console.WriteLine($"[Client] Received occupancy delta (tick {e.Delta.TickNumber}) with {e.Delta.Changes.Count} changes");

        foreach (var change in e.Delta.Changes)
        {
            Console.WriteLine($"[Client]   Occupancy change: ({change.X}, {change.Y}) occupied={change.Occupied}");

            // Update local validator
            _localValidator.UpdateLocalOccupancy(change.X, change.Y, change.Occupied, change.EntityId, change.StructureId);
        }
    }

    private void OnWorldSnapshotReceived(object? sender, WorldSnapshotReceivedEventArgs e)
    {
        Console.WriteLine($"[Client] Received world snapshot (tick {e.Snapshot.TickNumber})");
        Console.WriteLine($"[Client]   Placements: {e.Snapshot.Placements.Count}");
        Console.WriteLine($"[Client]   Occupancy: {e.Snapshot.Occupancy.Count}");

        // Update local validator with snapshot
        foreach (var occupancy in e.Snapshot.Occupancy)
        {
            _localValidator.UpdateLocalOccupancy(occupancy.X, occupancy.Y, occupancy.Occupied, occupancy.EntityId, occupancy.StructureId);
        }
    }

    private void OnPredictionMismatch(object? sender, PredictionMismatchEventArgs e)
    {
        Console.WriteLine($"[Client] ⚠️ Prediction mismatch!");
        Console.WriteLine($"[Client]   Predicted: {e.PredictedResult.Success}");
        Console.WriteLine($"[Client]   Server: {e.ServerResult.Success}");

        if (!e.ServerResult.Success)
        {
            Console.WriteLine($"[Client]   Reason: {e.ServerResult.FailureReason}");
        }
    }

    public void Stop()
    {
        _cancellationTokenSource.Cancel();
    }
}

/// <summary>
/// Entry point for the simple client example.
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        var host = args.Length > 0 ? args[0] : "127.0.0.1";
        var port = args.Length > 1 ? int.Parse(args[1]) : 7777;

        var client = new SimpleClientExample();

        // Handle Ctrl+C
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            client.Stop();
        };

        await client.RunAsync(host, port);
    }
}
