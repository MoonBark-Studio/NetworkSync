using MoonBark.NetworkSync.Core.Services;
using ServiceCellOccupancyData = MoonBark.NetworkSync.Core.Services.CellOccupancyData;
using MoonBark.NetworkSync.Core.Messages;

namespace MoonBark.NetworkSync.Tests.StressTests.Mocks;

/// <summary>
/// Thread-safe in-memory occupancy provider for stress testing.
/// Simulates Thistletide's core occupancy system.
/// </summary>
public class TestOccupancyProvider : ICoreOccupancyProvider
{
    private readonly Dictionary<(int x, int y), ServiceCellOccupancyData> _occupancyMap;
    private readonly object _lock = new();
    private readonly int _worldSize;
    private long _nextStructureId;

    public TestOccupancyProvider(int worldSize = 1000)
    {
        _occupancyMap = new Dictionary<(int, int), ServiceCellOccupancyData>();
        _worldSize = worldSize;
        _nextStructureId = 1;
    }

    public Task<bool> IsPlacementValidAsync(int x, int y, string structureType)
    {
        lock (_lock)
        {
            // Check bounds
            if (x < 0 || x >= _worldSize || y < 0 || y >= _worldSize)
            {
                return Task.FromResult(false);
            }

            // Check if already occupied
            if (_occupancyMap.ContainsKey((x, y)))
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }
    }

    public Task ApplyPlacementAsync(int x, int y, string structureType, int rotation)
    {
        lock (_lock)
        {
            var structureId = Interlocked.Increment(ref _nextStructureId);
            _occupancyMap[(x, y)] = new ServiceCellOccupancyData
            {
                Occupied = true,
                EntityId = null,
                StructureId = structureId
            };
        }

        return Task.CompletedTask;
    }

    public Task<ServiceCellOccupancyData> GetCellOccupancyAsync(int x, int y)
    {
        lock (_lock)
        {
            var occupancy = _occupancyMap.GetValueOrDefault((x, y), new ServiceCellOccupancyData { Occupied = false });
            return Task.FromResult(occupancy);
        }
    }

    public Task<RegionOccupancyData> GetRegionOccupancyAsync(int x, int y, int width, int height)
    {
        lock (_lock)
        {
            var region = new RegionOccupancyData
            {
                Cells = new List<ServiceCellOccupancyData>()
            };

            for (int dy = 0; dy < height; dy++)
            {
                for (int dx = 0; dx < width; dx++)
                {
                    var cellX = x + dx;
                    var cellY = y + dy;
                    var occupancy = _occupancyMap.GetValueOrDefault((cellX, cellY), new ServiceCellOccupancyData { Occupied = false });
                    region.Cells.Add(occupancy);
                }
            }

            return Task.FromResult(region);
        }
    }

    /// <summary>
    /// Gets a snapshot of all placements for verification.
    /// </summary>
    public Dictionary<(int x, int y), ServiceCellOccupancyData> GetAllPlacements()
    {
        lock (_lock)
        {
            return new Dictionary<(int, int), ServiceCellOccupancyData>(_occupancyMap);
        }
    }

    /// <summary>
    /// Gets the count of all placed structures.
    /// </summary>
    public int GetPlacementCount()
    {
        lock (_lock)
        {
            return _occupancyMap.Count;
        }
    }

    /// <summary>
    /// Clears all placements.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _occupancyMap.Clear();
        }
    }
}

/// <summary>
/// Thread-safe local validator for stress testing clients.
/// </summary>
public class TestLocalValidator : ILocalOccupancyValidator
{
    private readonly Dictionary<(int x, int y), ServiceCellOccupancyData> _localOccupancy;
    private readonly object _lock = new();
    private readonly int _worldSize;

    public TestLocalValidator(int worldSize = 1000)
    {
        _localOccupancy = new Dictionary<(int, int), ServiceCellOccupancyData>();
        _worldSize = worldSize;
    }

    public bool IsPlacementValidLocally(int x, int y, string structureType)
    {
        lock (_lock)
        {
            // Check bounds
            if (x < 0 || x >= _worldSize || y < 0 || y >= _worldSize)
            {
                return false;
            }

            // Check if already occupied locally
            if (_localOccupancy.ContainsKey((x, y)))
            {
                return false;
            }

            return true;
        }
    }

    public void UpdateLocalOccupancy(int x, int y, bool occupied, long? entityId = null, long? structureId = null)
    {
        lock (_lock)
        {
            if (occupied)
            {
                _localOccupancy[(x, y)] = new ServiceCellOccupancyData
                {
                    Occupied = true,
                    EntityId = entityId,
                    StructureId = structureId
                };
            }
            else
            {
                _localOccupancy.Remove((x, y));
            }
        }
    }

    /// <summary>
    /// Gets a snapshot of local occupancy for verification.
    /// </summary>
    public Dictionary<(int x, int y), ServiceCellOccupancyData> GetLocalOccupancy()
    {
        lock (_lock)
        {
            return new Dictionary<(int, int), ServiceCellOccupancyData>(_localOccupancy);
        }
    }

    /// <summary>
    /// Gets the count of locally tracked placements.
    /// </summary>
    public int GetLocalPlacementCount()
    {
        lock (_lock)
        {
            return _localOccupancy.Count;
        }
    }

    /// <summary>
    /// Clears local occupancy.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _localOccupancy.Clear();
        }
    }
}


