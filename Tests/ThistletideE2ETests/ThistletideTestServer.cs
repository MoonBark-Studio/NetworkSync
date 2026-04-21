using MoonBark.NetworkSync.Core.Interfaces;
using MoonBark.NetworkSync.Core.Messages;
using MoonBark.NetworkSync.Core.Services;
using CellOccupancyData = MoonBark.NetworkSync.Core.Services.CellOccupancyData;
using MoonBark.NetworkSync.Tests.ThistletideE2E.Components;
using MoonBark.NetworkSync.Tests.ThistletideE2E.ThistletideMock;

namespace MoonBark.NetworkSync.Tests.ThistletideE2E;

/// <summary>
/// Thistletide-specific test server using component-based architecture.
/// Implements ICoreOccupancyProvider to integrate with NetworkSync.
/// </summary>
/// <remarks>
/// This class is kept for backward compatibility but internally uses components.
/// For new code, prefer using ServerNodeComponent directly.
/// </remarks>
public class ThistletideTestServer : ICoreOccupancyProvider, IDisposable
{
    private readonly ServerNodeComponent _serverComponent;

    public bool IsRunning => _serverComponent.IsActive;
    public TrackedPlacementOccupancyMap OccupancyMap => _serverComponent.OccupancyMap.GetUnderlyingMap();
    public NetworkManager NetworkManager => _serverComponent.NetworkManager;

    public ThistletideTestServer(int gridWidth = 1000, int gridHeight = 1000, int port = 7777, int maxConnections = 100)
    {
        _serverComponent = new ServerNodeComponent(gridWidth, gridHeight, port, maxConnections);
    }

    public void Start()
    {
        _serverComponent.Start();
    }

    public void Stop()
    {
        _serverComponent.Stop();
    }

    public int GetPlacementCount()
    {
        return _serverComponent.OccupancyMap.GetOccupiedCount();
    }

    public Dictionary<(int x, int y), PlacementCellOccupancy> GetAllPlacements()
    {
        return _serverComponent.OccupancyMap.GetAllOccupiedCells();
    }

    // ICoreOccupancyProvider implementation
    public Task<bool> IsPlacementValidAsync(int x, int y, string structureType)
    {
        return _serverComponent.IsPlacementValidAsync(x, y, structureType);
    }

    public Task ApplyPlacementAsync(int x, int y, string structureType, int rotation)
    {
        return _serverComponent.ApplyPlacementAsync(x, y, structureType, rotation);
    }

    public Task<CellOccupancyData> GetCellOccupancyAsync(int x, int y)
    {
        return _serverComponent.GetCellOccupancyAsync(x, y);
    }

    public Task<RegionOccupancyData> GetRegionOccupancyAsync(int x, int y, int width, int height)
    {
        return _serverComponent.GetRegionOccupancyAsync(x, y, width, height);
    }

    public void Dispose()
    {
        _serverComponent.Dispose();
    }
}
