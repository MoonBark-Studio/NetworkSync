using NetworkSync.Core.Interfaces;
using NetworkSync.Core.Messages;
using NetworkSync.Core.Services;
using NetworkSync.Tests.ThistletideE2E.ThistletideMock;

namespace NetworkSync.Tests.ThistletideE2E;

/// <summary>
/// Thistletide-specific test client using TrackedPlacementOccupancyMap API.
/// Implements ILocalOccupancyValidator to integrate with NetworkSync.
/// </summary>
public class ThistletideTestClient : ILocalOccupancyValidator, IDisposable
{
    private readonly NetworkManager _networkManager;
    private readonly TrackedPlacementOccupancyMap _localOccupancyMap;
    private readonly int _gridWidth;
    private readonly int _gridHeight;
    private long _nextCommandId;
    private int _clientId;
    private bool _isConnected;

    public bool IsConnected => _isConnected;
    public int ClientId => _clientId;
    public TrackedPlacementOccupancyMap LocalOccupancyMap => _localOccupancyMap;
    public NetworkManager NetworkManager => _networkManager;

    public event EventHandler<PlacementResultMessage>? PlacementResultReceived;
    public event EventHandler<PlacementDeltaMessage>? PlacementDeltaReceived;

    public ThistletideTestClient(int gridWidth = 1000, int gridHeight = 1000)
    {
        _gridWidth = gridWidth;
        _gridHeight = gridHeight;
        _localOccupancyMap = new TrackedPlacementOccupancyMap(gridWidth, gridHeight);
        
        _networkManager = NetworkManager.CreateClient(this);
        
        // Subscribe to events
        _networkManager.Transport.MessageReceived += OnMessageReceived;
        _networkManager.ReplicationService.PlacementDeltaReceived += OnPlacementDelta;
        _networkManager.ReplicationService.OccupancyDeltaReceived += OnOccupancyDelta;
    }

    public async Task<bool> ConnectAsync(int clientId, string host = "127.0.0.1", int port = 7777, int timeoutMs = 10000)
    {
        _clientId = clientId;
        
        try
        {
            await _networkManager.ConnectAsync(host, port);
            _isConnected = _networkManager.IsConnected;
            return _isConnected;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ThistletideClient {ClientId}] Connection failed: {ex.Message}");
            return false;
        }
    }

    public async Task DisconnectAsync()
    {
        if (_isConnected)
        {
            await _networkManager.DisconnectAsync();
            _isConnected = false;
        }
    }

    public async Task<(bool success, long latencyMs)> SendPlacementCommandAsync(int x, int y, string structureType = "Wall", int rotation = 0)
    {
        if (!_isConnected)
        {
            return (false, 0);
        }

        var command = new PlacementCommandMessage
        {
            CommandId = _nextCommandId++,
            ClientId = _clientId,
            X = x,
            Y = y,
            StructureType = structureType,
            Rotation = rotation,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        var sendTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        try
        {
            var result = await _networkManager.SendPlacementCommandAsync(command);
            var latency = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - sendTime;
            return (result.Success, latency);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ThistletideClient {ClientId}] Send failed: {ex.Message}");
            return (false, 0);
        }
    }

    public int GetLocalPlacementCount()
    {
        int count = 0;
        for (int y = 0; y < _gridHeight; y++)
        {
            for (int x = 0; x < _gridWidth; x++)
            {
                if (_localOccupancyMap.IsOccupied(new CoreVector2I(x, y)))
                    count++;
            }
        }
        return count;
    }

    public Dictionary<(int x, int y), PlacementCellOccupancy> GetLocalPlacements()
    {
        var placements = new Dictionary<(int x, int y), PlacementCellOccupancy>();
        for (int y = 0; y < _gridHeight; y++)
        {
            for (int x = 0; x < _gridWidth; x++)
            {
                var pos = new CoreVector2I(x, y);
                if (_localOccupancyMap.IsOccupied(pos))
                {
                    placements[(x, y)] = _localOccupancyMap.GetCellOccupancy(pos);
                }
            }
        }
        return placements;
    }

    // ILocalOccupancyValidator implementation
    public bool IsPlacementValidLocally(int x, int y, string structureType)
    {
        // Check bounds
        if (x < 0 || x >= _gridWidth || y < 0 || y >= _gridHeight)
            return false;
        
        // Check local occupancy
        if (_localOccupancyMap.IsOccupied(new CoreVector2I(x, y)))
            return false;
        
        return true;
    }

    public void UpdateLocalOccupancy(int x, int y, bool occupied, long? entityId = null, long? structureId = null)
    {
        if (occupied)
        {
            _localOccupancyMap.SetCellOccupancy(
                new CoreVector2I(x, y), 
                true, 
                entityId ?? 0, 
                structureId ?? 0
            );
        }
        else
        {
            _localOccupancyMap.ClearCell(new CoreVector2I(x, y));
        }
    }

    private void OnMessageReceived(object? sender, NetworkMessageEventArgs e)
    {
        if (e.Message is PlacementResultMessage result)
        {
            PlacementResultReceived?.Invoke(this, result);
        }
    }

    private void OnPlacementDelta(object? sender, PlacementDeltaReceivedEventArgs e)
    {
        foreach (var change in e.Delta.Changes)
        {
            var pos = new CoreVector2I(change.X, change.Y);
            if (change.Type == PlacementDeltaMessage.ChangeType.Added)
            {
                _localOccupancyMap.SetCellOccupancy(pos, true, _clientId, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            }
            else if (change.Type == PlacementDeltaMessage.ChangeType.Removed)
            {
                _localOccupancyMap.ClearCell(pos);
            }
        }

        PlacementDeltaReceived?.Invoke(this, e.Delta);
    }

    private void OnOccupancyDelta(object? sender, OccupancyDeltaReceivedEventArgs e)
    {
        foreach (var change in e.Delta.Changes)
        {
            var pos = new CoreVector2I(change.X, change.Y);
            _localOccupancyMap.SetCellOccupancy(pos, change.Occupied, change.EntityId ?? 0, change.StructureId ?? 0);
        }
    }

    public void Dispose()
    {
        DisconnectAsync().Wait();
        _networkManager.Dispose();
    }
}
