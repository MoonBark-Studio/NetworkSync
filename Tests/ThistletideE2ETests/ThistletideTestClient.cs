using MoonBark.NetworkSync.Core.Interfaces;
using MoonBark.NetworkSync.Core.Messages;
using MoonBark.NetworkSync.Core.Services;
using MoonBark.NetworkSync.Tests.ThistletideE2E.Components;
using MoonBark.NetworkSync.Tests.ThistletideE2E.ThistletideMock;

namespace MoonBark.NetworkSync.Tests.ThistletideE2E;

/// <summary>
/// Thistletide-specific test client using component-based architecture.
/// Implements ILocalOccupancyValidator to integrate with NetworkSync.
/// </summary>
/// <remarks>
/// This class is kept for backward compatibility but internally uses components.
/// For new code, prefer using ClientNodeComponent directly.
/// </remarks>
public class ThistletideTestClient : ILocalOccupancyValidator, IDisposable
{
    private readonly ClientNodeComponent _clientComponent;

    public bool IsConnected => _clientComponent.IsActive;
    public int ClientId => _clientComponent.ClientId;
    public TrackedPlacementOccupancyMap LocalOccupancyMap => _clientComponent.LocalOccupancyMap.GetUnderlyingMap();
    public NetworkManager NetworkManager => _clientComponent.NetworkManager;

    public event EventHandler<PlacementResultMessage>? PlacementResultReceived
    {
        add => _clientComponent.PlacementResultReceived += value;
        remove => _clientComponent.PlacementResultReceived -= value;
    }

    public event EventHandler<PlacementDeltaMessage>? PlacementDeltaReceived
    {
        add => _clientComponent.PlacementDeltaReceived += value;
        remove => _clientComponent.PlacementDeltaReceived -= value;
    }

    public ThistletideTestClient(int gridWidth = 1000, int gridHeight = 1000)
    {
        _clientComponent = new ClientNodeComponent(gridWidth, gridHeight);
    }

    public async Task<bool> ConnectAsync(int clientId, string host = "127.0.0.1", int port = 7777, int timeoutMs = 10000)
    {
        return await _clientComponent.ConnectAsync(clientId, host, port, timeoutMs);
    }

    public async Task DisconnectAsync()
    {
        await _clientComponent.DisconnectAsync();
    }

    public async Task<(bool success, long latencyMs)> SendPlacementCommandAsync(int x, int y, string structureType = "Wall", int rotation = 0)
    {
        return await _clientComponent.SendPlacementCommandAsync(x, y, structureType, rotation);
    }

    public int GetLocalPlacementCount()
    {
        return _clientComponent.LocalOccupancyMap.GetOccupiedCount();
    }

    public Dictionary<(int x, int y), PlacementCellOccupancy> GetLocalPlacements()
    {
        return _clientComponent.LocalOccupancyMap.GetAllOccupiedCells();
    }

    // ILocalOccupancyValidator implementation
    public bool IsPlacementValidLocally(int x, int y, string structureType)
    {
        return _clientComponent.IsPlacementValidLocally(x, y, structureType);
    }

    public void UpdateLocalOccupancy(int x, int y, bool occupied, long? entityId = null, long? structureId = null)
    {
        _clientComponent.UpdateLocalOccupancy(x, y, occupied, entityId, structureId);
    }

    public void Dispose()
    {
        _clientComponent.Dispose();
    }
}
