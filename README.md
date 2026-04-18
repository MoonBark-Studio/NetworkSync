# NetworkSync Plugin

High-performance networking plugin for Godot games using LiteNetLib.

## Overview

NetworkSync provides a comprehensive networking solution built on LiteNetLib, optimized for real-time multiplayer games. It implements the delta-based replication architecture from the Thistletide networked placement architecture document.

See also:

- `ARCHITECTURE.md`
- `TEST_COVERAGE.md`

## Key Features

✅ **LiteNetLib Transport** - UDP-based networking with reliable/unreliable messaging
✅ **Delta Replication** - Efficient state synchronization using change deltas
✅ **Server Authority** - Authoritative server validation and state management
✅ **Client Prediction** - Local prediction with server reconciliation
✅ **Core-Authoritative** - Integrates with core C# simulation state
✅ **Battle-Tested** - Based on LiteNetLib, proven in production games

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     NetworkManager                          │
│  Coordinates all networking services                        │
└─────────────────────────────────────────────────────────────┘
                            │
        ┌───────────────────┼───────────────────┐
        │                   │                   │
        ▼                   ▼                   ▼
┌──────────────┐  ┌──────────────────┐  ┌──────────────────┐
│  Transport   │  │  Replication     │  │  Authority/      │
│  (LiteNetLib)│  │  Service         │  │  Prediction      │
└──────────────┘  └──────────────────┘  └──────────────────┘
        │                   │                   │
        └───────────────────┴───────────────────┘
                            │
                            ▼
                   ┌─────────────────┐
                   │  Core Occupancy │
                   │  (Game State)   │
                   └─────────────────┘
```

## Usage

### Server Setup

```csharp
using MoonBark.NetworkSync.Core.Services;
using MoonBark.NetworkSync.Core.Interfaces;

// Implement core occupancy provider
public class ThistletideOccupancyProvider : ICoreOccupancyProvider
{
    public async Task<bool> IsPlacementValidAsync(int x, int y, string structureType)
    {
        // Validate against your core occupancy state
        return await YourCoreSystem.IsPlacementValidAsync(x, y, structureType);
    }

    public async Task ApplyPlacementAsync(int x, int y, string structureType, int rotation)
    {
        // Apply placement to your core state
        await YourCoreSystem.ApplyPlacementAsync(x, y, structureType, rotation);
    }

    public async Task<CellOccupancyData> GetCellOccupancyAsync(int x, int y)
    {
        // Get occupancy from your core state
        return await YourCoreSystem.GetCellOccupancyAsync(x, y);
    }

    public async Task<RegionOccupancyData> GetRegionOccupancyAsync(int x, int y, int width, int height)
    {
        // Get region occupancy from your core state
        return await YourCoreSystem.GetRegionOccupancyAsync(x, y, width, height);
    }
}

// Create server
var occupancyProvider = new ThistletideOccupancyProvider();
var networkManager = NetworkManager.CreateServer(occupancyProvider, port: 7777, maxConnections: 10);

// Subscribe to events
networkManager.PeerConnected += (sender, e) =>
{
    Console.WriteLine($"Client {e.PeerId} connected from {e.EndPoint}");
};

networkManager.PeerDisconnected += (sender, e) =>
{
    Console.WriteLine($"Client {e.PeerId} disconnected: {e.Reason}");
};

// Subscribe to replication events
networkManager.ReplicationService.PlacementDeltaReceived += (sender, e) =>
{
    // Process placement delta (for server, this is when clients send updates)
    Console.WriteLine($"Received placement delta with {e.Delta.Changes.Count} changes");
};
```

### Client Setup

```csharp
using MoonBark.NetworkSync.Core.Services;
using MoonBark.NetworkSync.Core.Interfaces;

// Implement local occupancy validator
public class ThistletideLocalValidator : ILocalOccupancyValidator
{
    public bool IsPlacementValidLocally(int x, int y, string structureType)
    {
        // Validate against client-side occupancy state
        return YourLocalSystem.IsPlacementValid(x, y, structureType);
    }

    public void UpdateLocalOccupancy(int x, int y, bool occupied, long? entityId = null, long? structureId = null)
    {
        // Update local occupancy state
        YourLocalSystem.UpdateOccupancy(x, y, occupied, entityId, structureId);
    }
}

// Create client
var localValidator = new ThistletideLocalValidator();
var networkManager = NetworkManager.CreateClient(localValidator);

// Connect to server
await networkManager.ConnectAsync("127.0.0.1", 7777);

// Subscribe to replication events
networkManager.ReplicationService.PlacementDeltaReceived += (sender, e) =>
{
    // Process placement delta from server
    foreach (var change in e.Delta.Changes)
    {
        YourLocalSystem.ApplyPlacementChange(change);
    }
};

networkManager.ReplicationService.OccupancyDeltaReceived += (sender, e) =>
{
    // Process occupancy delta from server
    foreach (var change in e.Delta.Changes)
    {
        YourLocalSystem.UpdateOccupancy(change.X, change.Y, change.Occupied, change.EntityId, change.StructureId);
    }
};

// Subscribe to prediction events
networkManager.ClientPredictionService!.PredictionMismatch += (sender, e) =>
{
    Console.WriteLine($"Prediction mismatch! Server: {e.ServerResult.Success}, Predicted: {e.PredictedResult.Success}");
    // Handle reconciliation (rollback incorrect prediction)
};
```

### Sending Placement Commands

```csharp
// Client sends placement command
var command = new PlacementCommandMessage
{
    CommandId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
    ClientId = yourClientId,
    X = 10,
    Y = 20,
    StructureType = "Wall",
    Rotation = 0,
    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
};

// This predicts locally and sends to server
var predictedResult = await networkManager.SendPlacementCommandAsync(command);

// Show predicted result immediately (client prediction)
if (predictedResult.Success)
{
    ShowPlacementPreview(command.X, command.Y, command.StructureType);
}
else
{
    ShowValidationError(predictedResult.FailureReason);
}
```

### Publishing Deltas (Server)

```csharp
// Server publishes placement deltas
var placementDelta = new PlacementDeltaMessage();
networkManager.ReplicationService.TrackPlacementChange(
    structureId: 123,
    x: 10,
    y: 20,
    type: PlacementDeltaMessage.ChangeType.Added,
    structureType: "Wall",
    rotation: 0
);

// Publish to all clients
await networkManager.ReplicationService.PublishPlacementDeltaAsync(placementDelta);

// Server publishes occupancy deltas
var occupancyDelta = new OccupancyDeltaMessage();
networkManager.ReplicationService.TrackOccupancyChange(
    x: 10,
    y: 20,
    occupied: true,
    structureId: 123
);

// Publish to all clients
await networkManager.ReplicationService.PublishOccupancyDeltaAsync(occupancyDelta);
```

## Integration with Thistletide

The plugin integrates with Thistletide through a core occupancy abstraction. See [ARCHITECTURE.md](ARCHITECTURE.md#integration-points) for detailed integration examples and patterns.

## Test Coverage

The plugin includes comprehensive automated test coverage. See [TEST_COVERAGE.md](TEST_COVERAGE.md) for:
- Current coverage scope
- Test execution commands
- Coverage gaps and next steps

## Performance Characteristics

The plugin has been stress-tested under various load conditions. See [Tests/StressTests/NETWORK_SYNC_LIMITS.md](Tests/StressTests/NETWORK_SYNC_LIMITS.md) for:
- Detailed performance metrics
- Connection and throughput limits
- Latency characteristics
- Failure modes and boundaries

## Dependencies

- LiteNetLib 1.1.0
- System.Text.Json 8.0.5
- .NET 8.0

## License

MIT OR Apache-2.0

## Authors

MoonBark Studio
