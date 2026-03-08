# NetworkSync Architecture

## Overview

NetworkSync is a high-performance networking plugin built on LiteNetLib that implements the Thistletide networked placement architecture. It provides server-authoritative multiplayer with client prediction and delta-based replication.

## Design Principles

### 1. Core-Authoritative State

The core C# simulation is the source of truth for all game state:

```
Core C# Simulation (Authoritative)
    ↓ Delta Publication
NetworkSync Replication Service
    ↓ Network Transport
Godot Presentation Layer (Non-Authoritative)
```

### 2. Delta-Based Replication

Only changed cells are replicated, minimizing network traffic:

```csharp
// Bad: Full world sync
await SendFullWorldStateAsync(); // 500KB+ per tick

// Good: Delta replication
await SendPlacementDeltaAsync(changedCells); // <1KB per tick
```

### 3. Client Prediction with Reconciliation

Clients predict local state for responsiveness:

```
1. Client predicts placement locally
2. Client shows predicted result immediately
3. Client sends command to server
4. Server validates and applies to authoritative state
5. Server sends result to client
6. Client reconciles prediction with server result
```

## Component Architecture

### Transport Layer (LiteNetLib)

**Responsibility**: Low-level networking, connection management, message delivery

**Key Features**:
- UDP-based networking
- Reliable/unreliable messaging modes
- Built-in NAT traversal
- Automatic reconnection

**Implementation**: `LiteNetTransport.cs`

### Replication Service

**Responsibility**: Delta-based state synchronization

**Key Features**:
- Tracks changed cells
- Publishes placement/occupancy deltas
- Processes incoming deltas
- Manages tick numbers

**Implementation**: `ReplicationService.cs`

### Server Authority Service

**Responsibility**: Server-side validation and state management

**Key Features**:
- Validates placement commands against core state
- Applies validated commands to authoritative state
- Provides occupancy queries
- Caches command results

**Implementation**: `ServerAuthorityService.cs`

### Client Prediction Service

**Responsibility**: Client-side prediction and reconciliation

**Key Features**:
- Predicts placement results locally
- Reconciles with server results
- Manages predicted state
- Detects prediction mismatches

**Implementation**: `ClientPredictionService.cs`

### Network Manager

**Responsibility**: Coordinates all networking services

**Key Features**:
- Unified interface for server/client
- Manages service lifecycle
- Handles connection events
- Coordinates command processing

**Implementation**: `NetworkManager.cs`

## Message Flow

### Placement Command Flow

```
Client                          Server
  │                              │
  │ 1. Predict locally           │
  │ 2. Show predicted result     │
  │                              │
  │ 3. Send PlacementCommand    │
  ├─────────────────────────────>│
  │                              │ 4. Validate
  │                              │ 5. Apply to core state
  │                              │ 6. Track change
  │                              │
  │ 7. Send PlacementResult     │
  │<─────────────────────────────┤
  │                              │
  │ 8. Reconcile prediction      │
  │ 9. Update display            │
  │                              │
  │ 10. Broadcast PlacementDelta │
  │<─────────────────────────────┤
  │                              │
  │ 11. Update local state       │
  │ 12. Update Godot visuals     │
  │                              │
```

### Delta Replication Flow

```
Server                          Client
  │                              │
  │ 1. Track changed cells       │
  │ 2. Build PlacementDelta      │
  │                              │
  │ 3. Broadcast PlacementDelta  │
  ├─────────────────────────────>│
  │                              │ 4. Process delta
  │                              │ 5. Update core state
  │                              │ 6. Update Godot visuals
  │                              │
  │ 7. Broadcast OccupancyDelta  │
  ├─────────────────────────────>│
  │                              │ 8. Process delta
  │                              │ 9. Update local state
  │                              │ 10. Update Godot visuals
  │                              │
```

## Integration Points

### Core Occupancy Abstraction

The plugin requires a core occupancy abstraction:

```csharp
public interface ICoreOccupancyProvider
{
    Task<bool> IsPlacementValidAsync(int x, int y, string structureType);
    Task ApplyPlacementAsync(int x, int y, string structureType, int rotation);
    Task<CellOccupancyData> GetCellOccupancyAsync(int x, int y);
    Task<RegionOccupancyData> GetRegionOccupancyAsync(int x, int y, int width, int height);
}
```

### Local Occupancy Validation

Clients need a local validator for prediction:

```csharp
public interface ILocalOccupancyValidator
{
    bool IsPlacementValidLocally(int x, int y, string structureType);
    void UpdateLocalOccupancy(int x, int y, bool occupied, long? entityId = null, long? structureId = null);
}
```

### Godot Integration

The plugin integrates with Godot through the replication service:

```csharp
// In Godot bridge
public class CoreToGodotBridge
{
    private readonly NetworkManager _networkManager;

    public async Task UpdateGodotPresentationAsync()
    {
        var changedCells = _networkManager.ReplicationService.GetChangedCells();

        foreach (var (x, y) in changedCells)
        {
            UpdateGodotTile(x, y);
        }

        _networkManager.ReplicationService.ClearChangedCells();
    }
}
```

## Performance Optimizations

### 1. Delta-Based Updates

Only changed cells are replicated:

```
Full world sync: 500KB/tick
Delta replication: <1KB/tick
Improvement: 500x
```

### 2. Unreliable Delivery for High-Frequency Updates

Placement deltas use unreliable delivery for performance:

```csharp
// High-frequency updates (unreliable)
await BroadcastAsync(placementDelta, DeliveryMethod.Unreliable);

// Critical state (reliable)
await BroadcastAsync(occupancyDelta, DeliveryMethod.ReliableUnordered);
```

### 3. Message Batching

Multiple changes are batched into single delta messages:

```csharp
var delta = new PlacementDeltaMessage();
delta.Changes.AddRange(allChanges); // Batch all changes
await BroadcastAsync(delta, DeliveryMethod.Unreliable);
```

### 4. Tick-Based Synchronization

Deltas are tick-numbered for ordering:

```csharp
delta.TickNumber = ++_currentTick;
// Clients process deltas in tick order
```

## Security Considerations

### Server Authority

All state changes must be validated by the server:

```csharp
// Client cannot directly modify state
// All changes go through server validation
var result = await serverAuthority.ApplyPlacementCommandAsync(command);
```

### Command Validation

Server validates all placement commands:

```csharp
public async Task<bool> ValidatePlacementCommandAsync(PlacementCommandMessage command)
{
    // Check bounds
    // Check occupancy
    // Check permissions
    // Check game rules
    return isValid;
}
```

### Anti-Cheat

Server maintains authoritative state:

```csharp
// Client prediction is only for responsiveness
// Server state is always the source of truth
if (serverResult.Success != predictedResult.Success)
{
    // Reconcile: rollback incorrect prediction
    ReconcilePlacement(serverResult, predictedResult);
}
```

## Scalability

### Tested Scenarios

- 4 players
- 500+ entities
- 60 FPS
- <100ms latency

### Performance Limits

For detailed performance metrics, connection limits, and throughput characteristics, see [Tests/StressTests/NETWORK_SYNC_LIMITS.md](Tests/StressTests/NETWORK_SYNC_LIMITS.md).

## Future Enhancements

### Planned Features

1. **Chunk-Based Replication** - Terrain chunks for large worlds
2. **Entity Delta Replication** - Only changed entities
3. **HUD Delta Publication** - Optimized UI updates
4. **LOD and Culling** - Distance-aware visual policies
5. **Snapshot Compression** - Binary serialization for snapshots

### Optional Features

1. **Voice Chat** - Integrated voice communication
2. **Spectator Mode** - Read-only client connections
3. **Replay System** - Record and replay gameplay
4. **Dedicated Server** - Standalone server executable

## Troubleshooting

### Connection Issues

1. Check firewall settings
2. Verify port is not in use
3. Check NAT traversal settings
4. Review server logs for errors

### Performance Issues

1. Profile delta message sizes
2. Check tick rate
3. Monitor network bandwidth
4. Review prediction accuracy

### Prediction Mismatches

1. Check local validation logic
2. Verify server validation logic
3. Review occupancy state synchronization
4. Check tick synchronization

## Related Documentation

- [Networked Placement Architecture](../../games/thistletide/docs/development/networked-placement-architecture.md)
- [LiteNetLib Documentation](https://github.com/RevenantX/LiteNetLib)
- [Thistletide Roadmap](../../games/thistletide/ROADMAP.md)
