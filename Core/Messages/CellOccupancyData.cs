namespace MoonBark.NetworkSync.Core.Messages;

/// <summary>
/// Represents occupancy data for a single cell.
/// </summary>
public class CellOccupancyData
{
    public bool Occupied { get; set; }
    public long? EntityId { get; set; }
    public long? StructureId { get; set; }
}