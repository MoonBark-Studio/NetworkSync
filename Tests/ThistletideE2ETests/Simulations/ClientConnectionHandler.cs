using MoonBark.NetworkSync.Tests.ThistletideE2E.Components;

namespace MoonBark.NetworkSync.Tests.ThistletideE2E.Simulations;

/// <summary>
/// Handles client connections with configurable batch processing.
/// </summary>
public class ClientConnectionHandler
{
    private readonly List<ClientNodeComponent> _clients = new();
    private readonly int _gridWidth;
    private readonly int _gridHeight;
    private readonly int _batchSize;
    private readonly int _batchDelayMs;

    public IReadOnlyList<ClientNodeComponent> Clients => _clients.AsReadOnly();
    public int ConnectedCount => _clients.Count(c => c.IsActive);

    public ClientConnectionHandler(int gridWidth, int gridHeight, int batchSize = 10, int batchDelayMs = 100)
    {
        _gridWidth = gridWidth;
        _gridHeight = gridHeight;
        _batchSize = batchSize;
        _batchDelayMs = batchDelayMs;
    }

    public async Task<int> ConnectClientsAsync(
        int targetCount, 
        string host, 
        int port,
        Action<int, int>? onProgress = null)
    {
        Console.WriteLine($"[ClientConnectionHandler] Connecting {targetCount} clients...");

        for (int i = 0; i < targetCount; i++)
        {
            var client = new ClientNodeComponent(_gridWidth, _gridHeight);
            _clients.Add(client);

            var clientId = i + 1;
            await client.ConnectAsync(clientId, host, port);

            if ((i + 1) % _batchSize == 0 || i == targetCount - 1)
            {
                await Task.Delay(_batchDelayMs);
                onProgress?.Invoke(_clients.Count(c => c.IsActive), targetCount);
            }
        }

        return ConnectedCount;
    }

    public async Task DisconnectAllAsync()
    {
        foreach (var client in _clients)
        {
            try
            {
                await client.DisconnectAsync();
                client.Dispose();
            }
            catch { }
        }
        _clients.Clear();
    }

    public IEnumerable<ClientNodeComponent> GetActiveClients()
    {
        return _clients.Where(c => c.IsActive);
    }
}
