namespace MoonBark.NetworkSync.Core.Messages;

/// <summary>
/// Represents terrain data for a single cell.
/// </summary>
public class TerrainCellData
{
    public int LocalX { get; set; }
    public int LocalY { get; set; }
    public string TerrainType { get; set; } = string.Empty;
    public bool Blocked { get; set; }
}
