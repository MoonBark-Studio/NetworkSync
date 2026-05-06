namespace MoonBark.NetworkSync.Core.Interfaces;

/// <summary>
/// Defines the contract for delta-based state replication.
/// This service handles incremental state updates rather than full world sync.
/// </summary>
public interface IReplicationService
{
    /// <summary>
    /// Event raised when a placement delta is received from the server.
    /// </summary>
    event EventHandler<PlacementDeltaReceivedEventArgs> PlacementDeltaReceived;

    /// <summary>
    /// Event raised when an occupancy delta is received from the server.
    /// </summary>
    event EventHandler<OccupancyDeltaReceivedEventArgs> OccupancyDeltaReceived;

    /// <summary>
    /// Event raised when a world state snapshot is received.
    /// </summary>
    event EventHandler<WorldSnapshotReceivedEventArgs> WorldSnapshotReceived;

    /// <summary>
    /// Publishes a placement delta to connected clients (server-side).
    /// </summary>
    /// <param name="delta">The placement delta to publish.</param>
    Task PublishPlacementDeltaAsync(Messages.PlacementDeltaMessage delta);

    /// <summary>
    /// Publishes an occupancy delta to connected clients (server-side).
    /// </summary>
    /// <param name="delta">The occupancy delta to publish.</param>
    Task PublishOccupancyDeltaAsync(Messages.OccupancyDeltaMessage delta);

    /// <summary>
    /// Requests a full world state snapshot from the server (client-side).
    /// </summary>
    Task RequestWorldSnapshotAsync();

    /// <summary>
    /// Processes incoming placement delta and updates local state.
    /// </summary>
    /// <param name="delta">The placement delta to process.</param>
    Task ProcessPlacementDeltaAsync(Messages.PlacementDeltaMessage delta);

    /// <summary>
    /// Processes incoming occupancy delta and updates local state.
    /// </summary>
    /// <param name="delta">The occupancy delta to process.</param>
    Task ProcessOccupancyDeltaAsync(Messages.OccupancyDeltaMessage delta);

    /// <summary>
    /// Gets the set of cells that have changed since the last replication tick.
    /// </summary>
    /// <returns>A collection of changed cell coordinates.</returns>
    System.Collections.Generic.IEnumerable<(int x, int y)> GetChangedCells();

    /// <summary>
    /// Tracks a placement change for the next delta publish.
    /// </summary>
    /// <param name="structureId">The structure identifier for the change.</param>
    /// <param name="x">The X coordinate of the changed cell.</param>
    /// <param name="y">The Y coordinate of the changed cell.</param>
    /// <param name="type">The type of placement change.</param>
    /// <param name="structureType">The structure type for add or modify changes.</param>
    /// <param name="rotation">The structure rotation for add or modify changes.</param>
    void TrackPlacementChange(long structureId, int x, int y, Messages.PlacementDeltaChangeType type, string structureType = "", int rotation = 0);

    /// <summary>
    /// Tracks an occupancy change for the next delta publish.
    /// </summary>
    /// <param name="x">The X coordinate of the changed cell.</param>
    /// <param name="y">The Y coordinate of the changed cell.</param>
    /// <param name="occupied">Whether the cell is now occupied.</param>
    /// <param name="entityId">The occupying entity identifier, if any.</param>
    /// <param name="structureId">The occupying structure identifier, if any.</param>
    void TrackOccupancyChange(int x, int y, bool occupied, long? entityId = null, long? structureId = null);

    /// <summary>
    /// Clears tracked changed cells after consumers finish processing them.
    /// </summary>
    void ClearChangedCells();
}
