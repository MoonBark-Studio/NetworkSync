using MoonBark.NetworkSync.Core.Interfaces;

namespace MoonBark.NetworkSync.Core.Messages;

/// <summary>
/// Represents a complete world state snapshot.
/// Used for initial client connection and reconciliation.
/// </summary>
public class WorldSnapshotMessage : NetworkMessageBase
{
    public override byte MessageType => MessageTypes.WorldSnapshot;

    /// <summary>
    /// The tick number this snapshot corresponds to.
    /// </summary>
    public long TickNumber { get; set; }

    /// <summary>
    /// The server timestamp when the snapshot was taken.
    /// </summary>
    public long ServerTimestamp { get; set; }

    /// <summary>
    /// The placement data in this snapshot.
    /// </summary>
    public List<PlacementSnapshotEntry> Placements { get; set; } = new();

    /// <summary>
    /// The occupancy data in this snapshot.
    /// </summary>
    public List<OccupancySnapshotEntry> Occupancy { get; set; } = new();

    /// <summary>
    /// Represents a placement entry in a snapshot.
    /// </summary>
    public class PlacementSnapshotEntry
    {
        public int X { get; set; }
        public int Y { get; set; }
        public string StructureType { get; set; } = string.Empty;
        public int Rotation { get; set; }
        public long StructureId { get; set; }
    }

    /// <summary>
    /// Represents an occupancy entry in a snapshot.
    /// </summary>
    public class OccupancySnapshotEntry
    {
        public int X { get; set; }
        public int Y { get; set; }
        public bool Occupied { get; set; }
        public long? EntityId { get; set; }
        public long? StructureId { get; set; }
    }
}

/// <summary>
/// Represents a delta for a chunk of terrain.
/// Used for efficient terrain updates.
/// </summary>
public class ChunkDeltaMessage : NetworkMessageBase
{
    public override byte MessageType => MessageTypes.ChunkDelta;

    /// <summary>
    /// The chunk X coordinate.
    /// </summary>
    public int ChunkX { get; set; }

    /// <summary>
    /// The chunk Y coordinate.
    /// </summary>
    public int ChunkY { get; set; }

    /// <summary>
    /// The size of the chunk (in cells).
    /// </summary>
    public int ChunkSize { get; set; }

    /// <summary>
    /// The terrain data for the chunk.
    /// </summary>
    public List<TerrainCellData> Terrain { get; set; } = new();
}

/// <summary>
/// Represents a predicted placement on the client.
/// </summary>
public class PredictedPlacementMessage : NetworkMessageBase
{
    public override byte MessageType => MessageTypes.PredictedPlacement;

    /// <summary>
    /// The command ID this prediction corresponds to.
    /// </summary>
    public long CommandId { get; set; }

    /// <summary>
    /// The X coordinate of the predicted placement.
    /// </summary>
    public int X { get; set; }

    /// <summary>
    /// The Y coordinate of the predicted placement.
    /// </summary>
    public int Y { get; set; }

    /// <summary>
    /// The structure type being predicted.
    /// </summary>
    public string StructureType { get; set; } = string.Empty;

    /// <summary>
    /// The rotation of the structure.
    /// </summary>
    public int Rotation { get; set; }

    /// <summary>
    /// Whether the prediction is valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// The validation reason if invalid.
    /// </summary>
    public string ValidationReason { get; set; } = string.Empty;
}

/// <summary>
/// Represents a prediction reconciliation event.
/// </summary>
public class PredictionReconcileMessage : NetworkMessageBase
{
    public override byte MessageType => MessageTypes.PredictionReconcile;

    /// <summary>
    /// The command ID to reconcile.
    /// </summary>
    public long CommandId { get; set; }

    /// <summary>
    /// The authoritative server result.
    /// </summary>
    public PlacementResultMessage ServerResult { get; set; } = null!;

    /// <summary>
    /// Whether the prediction was correct.
    /// </summary>
    public bool PredictionCorrect { get; set; }
}
