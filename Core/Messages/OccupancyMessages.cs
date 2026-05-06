using MoonBark.NetworkSync.Core.Interfaces;

namespace MoonBark.NetworkSync.Core.Messages;

/// <summary>
/// Represents a delta of placement changes to replicate.
/// Contains only changed cells to minimize network traffic.
/// </summary>
public class PlacementDeltaMessage : NetworkMessageBase
{
    public override byte MessageType => MessageTypes.PlacementDelta;

    /// <summary>
    /// The tick number this delta corresponds to.
    /// </summary>
    public long TickNumber { get; set; }

    /// <summary>
    /// The list of placement changes in this delta.
    /// </summary>
    public List<PlacementChange> Changes { get; set; } = new();

    /// <summary>
    /// Represents a single placement change.
    /// </summary>
    public class PlacementChange
    {
        /// <summary>
        /// The X coordinate of the change.
        /// </summary>
        public int X { get; set; }

        /// <summary>
        /// The Y coordinate of the change.
        /// </summary>
        public int Y { get; set; }

        /// <summary>
        /// The type of change (add/remove).
        /// </summary>
        public PlacementDeltaChangeType Type { get; set; }

        /// <summary>
        /// The structure type (for additions).
        /// </summary>
        public string StructureType { get; set; } = string.Empty;

        /// <summary>
        /// The rotation of the structure.
        /// </summary>
        public int Rotation { get; set; }
    }

}

/// <summary>
/// Represents a delta of occupancy changes to replicate.
/// Contains only changed cells to minimize network traffic.
/// </summary>
public class OccupancyDeltaMessage : NetworkMessageBase
{
    public override byte MessageType => MessageTypes.OccupancyDelta;

    /// <summary>
    /// The tick number this delta corresponds to.
    /// </summary>
    public long TickNumber { get; set; }

    /// <summary>
    /// The list of occupancy changes in this delta.
    /// </summary>
    public List<OccupancyChange> Changes { get; set; } = new();

    /// <summary>
    /// Represents a single occupancy change.
    /// </summary>
    public class OccupancyChange
    {
        /// <summary>
        /// The X coordinate of the change.
        /// </summary>
        public int X { get; set; }

        /// <summary>
        /// The Y coordinate of the change.
        /// </summary>
        public int Y { get; set; }

        /// <summary>
        /// Whether the cell is now occupied.
        /// </summary>
        public bool Occupied { get; set; }

        /// <summary>
        /// The entity ID occupying the cell (if any).
        /// </summary>
        public long? EntityId { get; set; }

        /// <summary>
        /// The structure ID occupying the cell (if any).
        /// </summary>
        public long? StructureId { get; set; }
    }
}

/// <summary>
/// Represents the occupancy of a single cell.
/// </summary>
public class CellOccupancyMessage : NetworkMessageBase
{
    public override byte MessageType => MessageTypes.CellOccupancy;

    /// <summary>
    /// The X coordinate of the cell.
    /// </summary>
    public int X { get; set; }

    /// <summary>
    /// The Y coordinate of the cell.
    /// </summary>
    public int Y { get; set; }

    /// <summary>
    /// Whether the cell is occupied.
    /// </summary>
    public bool Occupied { get; set; }

    /// <summary>
    /// The entity ID occupying the cell (if any).
    /// </summary>
    public long? EntityId { get; set; }

    /// <summary>
    /// The structure ID occupying the cell (if any).
    /// </summary>
    public long? StructureId { get; set; }
}

/// <summary>
/// Represents the occupancy of a region of cells.
/// </summary>
public class RegionOccupancyMessage : NetworkMessageBase
{
    public override byte MessageType => MessageTypes.RegionOccupancy;

    /// <summary>
    /// The starting X coordinate of the region.
    /// </summary>
    public int X { get; set; }

    /// <summary>
    /// The starting Y coordinate of the region.
    /// </summary>
    public int Y { get; set; }

    /// <summary>
    /// The width of the region.
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// The height of the region.
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// The occupancy data for the region (row-major order).
    /// </summary>
    public List<CellOccupancyData> Cells { get; set; } = new();
}
