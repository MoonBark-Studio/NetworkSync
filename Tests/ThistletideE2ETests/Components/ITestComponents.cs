using MoonBark.NetworkSync.Core.Interfaces;
using MoonBark.NetworkSync.Core.Services;
using MoonBark.NetworkSync.Tests.ThistletideE2E.ThistletideMock;

namespace MoonBark.NetworkSync.Tests.ThistletideE2E.Components;

/// <summary>
/// Component interface for occupancy map operations.
/// Provides abstraction over grid-based occupancy tracking.
/// </summary>
public interface IOccupancyMapComponent
{
    /// <summary>Gets the width of the occupancy grid.</summary>
    int Width { get; }
    
    /// <summary>Gets the height of the occupancy grid.</summary>
    int Height { get; }
    
    /// <summary>Checks if a cell is occupied.</summary>
    bool IsOccupied(CoreVector2I position);
    
    /// <summary>Gets the occupancy data for a cell.</summary>
    PlacementCellOccupancy GetCellOccupancy(CoreVector2I position);
    
    /// <summary>Sets the occupancy for a cell.</summary>
    void SetCellOccupancy(CoreVector2I position, bool occupied, long entityId = 0, long structureId = 0);
    
    /// <summary>Clears a cell's occupancy.</summary>
    void ClearCell(CoreVector2I position);
    
    /// <summary>Clears all cells.</summary>
    void Clear();
    
    /// <summary>Gets all occupied cells as a dictionary.</summary>
    Dictionary<(int x, int y), PlacementCellOccupancy> GetAllOccupiedCells();
    
    /// <summary>Gets the count of occupied cells.</summary>
    int GetOccupiedCount();
}

/// <summary>
/// Component interface for placement validation.
/// </summary>
public interface IPlacementValidationComponent
{
    /// <summary>Validates if a placement is valid at the given position.</summary>
    Task<bool> ValidatePlacementAsync(int x, int y, string structureType);
    
    /// <summary>Applies a placement at the given position.</summary>
    Task ApplyPlacementAsync(int x, int y, string structureType, int rotation = 0);
}

/// <summary>
/// Component interface for network node operations.
/// </summary>
public interface INetworkNodeComponent : IDisposable
{
    /// <summary>Gets whether the node is currently connected/active.</summary>
    bool IsActive { get; }
    
    /// <summary>Gets the network manager instance.</summary>
    NetworkManager NetworkManager { get; }
}

/// <summary>
/// Component interface for server-specific operations.
/// </summary>
public interface IServerNodeComponent : INetworkNodeComponent, ICoreOccupancyProvider
{
    /// <summary>Starts the server.</summary>
    void Start();
    
    /// <summary>Stops the server.</summary>
    void Stop();
}

/// <summary>
/// Component interface for client-specific operations.
/// </summary>
public interface IClientNodeComponent : INetworkNodeComponent, ILocalOccupancyValidator
{
    /// <summary>Gets the client ID.</summary>
    int ClientId { get; }
    
    /// <summary>Connects to a server.</summary>
    Task<bool> ConnectAsync(int clientId, string host = "127.0.0.1", int port = 7777, int timeoutMs = 10000);
    
    /// <summary>Disconnects from the server.</summary>
    Task DisconnectAsync();
    
    /// <summary>Sends a placement command to the server.</summary>
    Task<(bool success, long latencyMs)> SendPlacementCommandAsync(int x, int y, string structureType = "Wall", int rotation = 0);
}

/// <summary>
/// Factory for creating test components.
/// </summary>
public interface ITestComponentFactory
{
    /// <summary>Creates an occupancy map component.</summary>
    IOccupancyMapComponent CreateOccupancyMap(int width, int height);
    
    /// <summary>Creates a server node component.</summary>
    IServerNodeComponent CreateServerNode(int gridWidth, int gridHeight, int port = 7777, int maxConnections = 100);
    
    /// <summary>Creates a client node component.</summary>
    IClientNodeComponent CreateClientNode(int gridWidth, int gridHeight);
}
