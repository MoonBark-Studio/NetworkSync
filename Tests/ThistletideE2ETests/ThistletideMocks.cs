namespace NetworkSync.Tests.ThistletideE2E.ThistletideMock;

/// <summary>
/// Mock implementation of Thistletide's TrackedPlacementOccupancyMap for testing.
/// This mimics the real Thistletide.Core.Placement.TrackedPlacementOccupancyMap API.
/// </summary>
public class TrackedPlacementOccupancyMap
{
    private readonly bool[,] _occupancyGrid;
    private readonly long[,] _entityIds;
    private readonly long[,] _structureIds;
    private readonly int _width;
    private readonly int _height;

    public int Width => _width;
    public int Height => _height;

    public TrackedPlacementOccupancyMap(int width, int height)
    {
        _width = width;
        _height = height;
        _occupancyGrid = new bool[width, height];
        _entityIds = new long[width, height];
        _structureIds = new long[width, height];
    }

    public bool IsOccupied(CoreVector2I position)
    {
        if (position.X < 0 || position.X >= _width || position.Y < 0 || position.Y >= _height)
            return false;
        return _occupancyGrid[position.X, position.Y];
    }

    public PlacementCellOccupancy GetCellOccupancy(CoreVector2I position)
    {
        if (position.X < 0 || position.X >= _width || position.Y < 0 || position.Y >= _height)
            return PlacementCellOccupancy.Empty;
        
        return new PlacementCellOccupancy
        {
            X = position.X,
            Y = position.Y,
            Occupied = _occupancyGrid[position.X, position.Y],
            EntityId = _entityIds[position.X, position.Y],
            StructureId = _structureIds[position.X, position.Y]
        };
    }

    public void SetCellOccupancy(CoreVector2I position, bool occupied, long entityId = 0, long structureId = 0)
    {
        if (position.X < 0 || position.X >= _width || position.Y < 0 || position.Y >= _height)
            return;
        
        _occupancyGrid[position.X, position.Y] = occupied;
        _entityIds[position.X, position.Y] = entityId;
        _structureIds[position.X, position.Y] = structureId;
    }

    public void ClearCell(CoreVector2I position)
    {
        if (position.X < 0 || position.X >= _width || position.Y < 0 || position.Y >= _height)
            return;
        
        _occupancyGrid[position.X, position.Y] = false;
        _entityIds[position.X, position.Y] = 0;
        _structureIds[position.X, position.Y] = 0;
    }

    public void Clear()
    {
        Array.Clear(_occupancyGrid, 0, _occupancyGrid.Length);
        Array.Clear(_entityIds, 0, _entityIds.Length);
        Array.Clear(_structureIds, 0, _structureIds.Length);
    }
}

/// <summary>
/// Cell occupancy data structure matching Thistletide's PlacementCellOccupancy.
/// </summary>
public struct PlacementCellOccupancy
{
    public int X { get; set; }
    public int Y { get; set; }
    public bool Occupied { get; set; }
    public long EntityId { get; set; }
    public long StructureId { get; set; }

    public static PlacementCellOccupancy Empty => new()
    {
        X = 0,
        Y = 0,
        Occupied = false,
        EntityId = 0,
        StructureId = 0
    };
}

/// <summary>
/// Simple 2D vector for integers matching MoonBark.Framework.Types.CoreVector2I.
/// </summary>
public struct CoreVector2I
{
    public int X { get; set; }
    public int Y { get; set; }

    public CoreVector2I(int x, int y)
    {
        X = x;
        Y = y;
    }

    public override bool Equals(object? obj)
    {
        if (obj is CoreVector2I other)
            return X == other.X && Y == other.Y;
        return false;
    }

    public override int GetHashCode() => HashCode.Combine(X, Y);

    public static bool operator ==(CoreVector2I left, CoreVector2I right) => left.X == right.X && left.Y == right.Y;
    public static bool operator !=(CoreVector2I left, CoreVector2I right) => !(left == right);
}
