using MoonBark.NetworkSync.Tests.ThistletideE2E.ThistletideMock;

namespace MoonBark.NetworkSync.Tests.ThistletideE2E.Components;

/// <summary>
/// Component implementation for occupancy map operations.
/// Wraps TrackedPlacementOccupancyMap to provide the IOccupancyMapComponent interface.
/// </summary>
public class OccupancyMapComponent : IOccupancyMapComponent
{
    private readonly TrackedPlacementOccupancyMap _occupancyMap;

    public int Width => _occupancyMap.Width;
    public int Height => _occupancyMap.Height;

    public OccupancyMapComponent(int width, int height)
    {
        _occupancyMap = new TrackedPlacementOccupancyMap(width, height);
    }

    /// <summary>
    /// Internal constructor for wrapping an existing occupancy map.
    /// </summary>
    internal OccupancyMapComponent(TrackedPlacementOccupancyMap existingMap)
    {
        _occupancyMap = existingMap ?? throw new ArgumentNullException(nameof(existingMap));
    }

    public bool IsOccupied(CoreVector2I position)
    {
        return _occupancyMap.IsOccupied(position);
    }

    public PlacementCellOccupancy GetCellOccupancy(CoreVector2I position)
    {
        return _occupancyMap.GetCellOccupancy(position);
    }

    public void SetCellOccupancy(CoreVector2I position, bool occupied, long entityId = 0, long structureId = 0)
    {
        _occupancyMap.SetCellOccupancy(position, occupied, entityId, structureId);
    }

    public void ClearCell(CoreVector2I position)
    {
        _occupancyMap.ClearCell(position);
    }

    public void Clear()
    {
        _occupancyMap.Clear();
    }

    public Dictionary<(int x, int y), PlacementCellOccupancy> GetAllOccupiedCells()
    {
        var placements = new Dictionary<(int x, int y), PlacementCellOccupancy>();
        
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                var pos = new CoreVector2I(x, y);
                if (_occupancyMap.IsOccupied(pos))
                {
                    placements[(x, y)] = _occupancyMap.GetCellOccupancy(pos);
                }
            }
        }
        
        return placements;
    }

    public int GetOccupiedCount()
    {
        int count = 0;
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (_occupancyMap.IsOccupied(new CoreVector2I(x, y)))
                    count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Gets the underlying occupancy map for advanced operations.
    /// </summary>
    public TrackedPlacementOccupancyMap GetUnderlyingMap() => _occupancyMap;
}
