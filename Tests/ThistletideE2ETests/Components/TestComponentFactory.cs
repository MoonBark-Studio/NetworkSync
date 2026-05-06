using MoonBark.NetworkSync.Tests.ThistletideE2E.Components;

namespace MoonBark.NetworkSync.Tests.ThistletideE2E;

/// <summary>
/// Factory for creating test components with dependency injection.
/// </summary>
public class TestComponentFactory : ITestComponentFactory
{
    private readonly Dictionary<string, IOccupancyMapComponent> _occupancyMaps = new();
    private readonly List<ServerNodeComponent> _servers = new();
    private readonly List<ClientNodeComponent> _clients = new();

    public IOccupancyMapComponent CreateOccupancyMap(int width, int height)
    {
        var key = $"{width}x{height}";
        
        if (!_occupancyMaps.TryGetValue(key, out var map))
        {
            map = new OccupancyMapComponent(width, height);
            _occupancyMaps[key] = map;
        }
        
        return map;
    }

    public IServerNodeComponent CreateServerNode(int gridWidth, int gridHeight, int port = 7777, int maxConnections = 100)
    {
        var server = new ServerNodeComponent(gridWidth, gridHeight, port, maxConnections);
        _servers.Add(server);
        return server;
    }

    public IClientNodeComponent CreateClientNode(int gridWidth, int gridHeight)
    {
        var client = new ClientNodeComponent(gridWidth, gridHeight);
        _clients.Add(client);
        return client;
    }

    /// <summary>
    /// Disposes all created components.
    /// </summary>
    public void Dispose()
    {
        foreach (var client in _clients)
        {
            try { client.Dispose(); } catch { }
        }
        _clients.Clear();

        foreach (var server in _servers)
        {
            try { server.Dispose(); } catch { }
        }
        _servers.Clear();

        _occupancyMaps.Clear();
    }
}
