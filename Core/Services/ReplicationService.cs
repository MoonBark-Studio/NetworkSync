using MoonBark.Framework.Logging;
using MoonBark.NetworkSync.Core.Interfaces;
using MoonBark.NetworkSync.Core.Messages;

namespace MoonBark.NetworkSync.Core.Services;

/// <summary>
/// Delta-based replication service for efficient state synchronization.
/// Implements the one-way projection pattern from core state to clients.
/// </summary>
public class ReplicationService : IReplicationService
{
    private readonly INetworkTransport _transport;
    private readonly HashSet<(int x, int y)> _changedCells;
    private readonly Dictionary<long, PlacementDeltaMessage.PlacementChange> _pendingPlacementChanges;
    private readonly Dictionary<long, OccupancyDeltaMessage.OccupancyChange> _pendingOccupancyChanges;
    private long _currentTick;
    private readonly object _lock = new();

    public event EventHandler<PlacementDeltaReceivedEventArgs>? PlacementDeltaReceived;
    public event EventHandler<OccupancyDeltaReceivedEventArgs>? OccupancyDeltaReceived;
    public event EventHandler<WorldSnapshotReceivedEventArgs>? WorldSnapshotReceived;

    private bool _disposed;

    private readonly IFrameworkLogger _logger;

    public ReplicationService(INetworkTransport transport)
        : this(transport, new ConsoleFrameworkLogger("ReplicationService", FrameworkLogLevel.Debug)) { }

    public ReplicationService(INetworkTransport transport, IFrameworkLogger logger)
    {
        _logger = logger;
        _transport = transport;
        _changedCells = new HashSet<(int, int)>();
        _pendingPlacementChanges = new Dictionary<long, PlacementDeltaMessage.PlacementChange>();
        _pendingOccupancyChanges = new Dictionary<long, OccupancyDeltaMessage.OccupancyChange>();
        _currentTick = 0;

        // Subscribe to transport events
        _transport.MessageReceived += OnMessageReceived;
    }

    /// <summary>
    /// Tracks a cell as changed for delta replication.
    /// </summary>
    public void MarkCellChanged(int x, int y)
    {
        lock (_lock)
        {
            _changedCells.Add((x, y));
        }
    }

    /// <summary>
    /// Tracks a placement change for delta replication.
    /// </summary>
    public void TrackPlacementChange(long structureId, int x, int y, PlacementDeltaMessage.ChangeType type, string structureType = "", int rotation = 0)
    {
        lock (_lock)
        {
            var change = new PlacementDeltaMessage.PlacementChange
            {
                X = x,
                Y = y,
                Type = type,
                StructureType = structureType,
                Rotation = rotation
            };

            _pendingPlacementChanges[structureId] = change;
            MarkCellChanged(x, y);
        }
    }

    /// <summary>
    /// Tracks an occupancy change for delta replication.
    /// </summary>
    public void TrackOccupancyChange(int x, int y, bool occupied, long? entityId = null, long? structureId = null)
    {
        lock (_lock)
        {
            var key = ((long)x << 32) | (uint)y;
            var change = new OccupancyDeltaMessage.OccupancyChange
            {
                X = x,
                Y = y,
                Occupied = occupied,
                EntityId = entityId,
                StructureId = structureId
            };

            _pendingOccupancyChanges[key] = change;
            MarkCellChanged(x, y);
        }
    }

    public async Task PublishPlacementDeltaAsync(PlacementDeltaMessage delta)
    {
        if (!_transport.IsConnected)
        {
            _logger.Warning("Cannot publish placement delta: not connected");
            return;
        }

        lock (_lock)
        {
            delta.TickNumber = ++_currentTick;
            delta.Changes.Clear();
            delta.Changes.AddRange(_pendingPlacementChanges.Values);
            _pendingPlacementChanges.Clear();
        }

        // Broadcast to all clients (unreliable for high-frequency updates)
        await _transport.BroadcastAsync(delta, DeliveryMethod.Unreliable);
        _logger.Debug($"Published placement delta for tick {delta.TickNumber} with {delta.Changes.Count} changes");
    }

    public async Task PublishOccupancyDeltaAsync(OccupancyDeltaMessage delta)
    {
        if (!_transport.IsConnected)
        {
            _logger.Warning("Cannot publish occupancy delta: not connected");
            return;
        }

        lock (_lock)
        {
            delta.TickNumber = ++_currentTick;
            delta.Changes.Clear();
            delta.Changes.AddRange(_pendingOccupancyChanges.Values);
            _pendingOccupancyChanges.Clear();
        }

        // Broadcast to all clients (reliable for occupancy state)
        await _transport.BroadcastAsync(delta, DeliveryMethod.ReliableUnordered);
        _logger.Debug($"Published occupancy delta for tick {delta.TickNumber} with {delta.Changes.Count} changes");
    }

    public async Task RequestWorldSnapshotAsync()
    {
        if (!_transport.IsConnected)
        {
            _logger.Warning("Cannot request snapshot: not connected");
            return;
        }

        // Send snapshot request to server
        var request = new WorldSnapshotMessage
        {
            TickNumber = _currentTick,
            ServerTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        // In a real implementation, this would be a dedicated request message
        // For now, we'll send the snapshot message as a request
        await _transport.SendAsync(0, request, DeliveryMethod.ReliableOrdered);
        _logger.Debug("Requested world snapshot");
    }

    public async Task ProcessPlacementDeltaAsync(PlacementDeltaMessage delta)
    {
        lock (_lock)
        {
            // Update local state with placement changes
            foreach (var change in delta.Changes)
            {
                MarkCellChanged(change.X, change.Y);
                _logger.Trace($"Processing placement change: ({change.X}, {change.Y}) {change.Type}");
            }
        }

        // Notify subscribers
        PlacementDeltaReceived?.Invoke(this, new PlacementDeltaReceivedEventArgs { Delta = delta });
    }

    public async Task ProcessOccupancyDeltaAsync(OccupancyDeltaMessage delta)
    {
        lock (_lock)
        {
            // Update local state with occupancy changes
            foreach (var change in delta.Changes)
            {
                MarkCellChanged(change.X, change.Y);
                _logger.Trace($"Processing occupancy change: ({change.X}, {change.Y}) occupied={change.Occupied}");
            }
        }

        // Notify subscribers
        OccupancyDeltaReceived?.Invoke(this, new OccupancyDeltaReceivedEventArgs { Delta = delta });
    }

    public System.Collections.Generic.IEnumerable<(int x, int y)> GetChangedCells()
    {
        lock (_lock)
        {
            return _changedCells.ToList();
        }
    }

    public void ClearChangedCells()
    {
        lock (_lock)
        {
            _changedCells.Clear();
        }
    }

    /// <summary>
    /// Detaches from transport events and releases resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _transport.MessageReceived -= OnMessageReceived;
    }

    private void OnMessageReceived(object? sender, NetworkMessageEventArgs e)
    {
        switch (e.Message)
        {
            case PlacementDeltaMessage placementDelta:
                Task.Run(() => ProcessPlacementDeltaAsync(placementDelta));
                break;

            case OccupancyDeltaMessage occupancyDelta:
                Task.Run(() => ProcessOccupancyDeltaAsync(occupancyDelta));
                break;

            case WorldSnapshotMessage snapshot:
                WorldSnapshotReceived?.Invoke(this, new WorldSnapshotReceivedEventArgs { Snapshot = snapshot });
                _logger.Info($"Received world snapshot for tick {snapshot.TickNumber}");
                break;
        }
    }
}
