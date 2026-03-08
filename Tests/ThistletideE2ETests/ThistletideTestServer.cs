using NetworkSync.Core.Interfaces;
using NetworkSync.Core.Messages;
using NetworkSync.Core.Services;
using NetworkSync.Tests.ThistletideE2E.ThistletideMock;

namespace NetworkSync.Tests.ThistletideE2E;

/// <summary>
/// Thistletide-specific test server using TrackedPlacementOccupancyMap API.
/// Implements ICoreOccupancyProvider to integrate with NetworkSync.
/// </summary>
public class ThistletideTestServer : ICoreOccupancyProvider, IDisposable
{
    private readonly NetworkManager _networkManager;
    private readonly TrackedPlacementOccupancyMap _serverOccupancyMap;
    private readonly int _gridWidth;
    private readonly int _gridHeight;
    private long _nextStructureId = 1;
    private bool _isRunning;

    public bool IsRunning => _isRunning;
    public TrackedPlacementOccupancyMap OccupancyMap => _serverOccupancyMap;
    public NetworkManager NetworkManager => _networkManager;

    public ThistletideTestServer(int gridWidth = 1000, int gridHeight = 1000, int port = 7777, int maxConnections = 100)
    {
        _gridWidth = gridWidth;
        _gridHeight = gridHeight;
        _serverOccupancyMap = new TrackedPlacementOccupancyMap(gridWidth, gridHeight);
        
        _networkManager = NetworkManager.CreateServer(this, port, maxConnections);
        
        // Subscribe to events
        _networkManager.Transport.MessageReceived += OnMessageReceived;
    }

    public void Start()
    {
        _isRunning = true;
    }

    public void Stop()
    {
        _isRunning = false;
    }

    public int GetPlacementCount()
    {
        int count = 0;
        for (int y = 0; y < _gridHeight; y++)
        {
            for (int x = 0; x < _gridWidth; x++)
            {
                if (_serverOccupancyMap.IsOccupied(new CoreVector2I(x, y)))
                    count++;
            }
        }
        return count;
    }

    public Dictionary<(int x, int y), PlacementCellOccupancy> GetAllPlacements()
    {
        var placements = new Dictionary<(int x, int y), PlacementCellOccupancy>();
        for (int y = 0; y < _gridHeight; y++)
        {
            for (int x = 0; x < _gridWidth; x++)
            {
                var pos = new CoreVector2I(x, y);
                if (_serverOccupancyMap.IsOccupied(pos))
                {
                    placements[(x, y)] = _serverOccupancyMap.GetCellOccupancy(pos);
                }
            }
        }
        return placements;
    }

    // ICoreOccupancyProvider implementation
    public Task<bool> IsPlacementValidAsync(int x, int y, string structureType)
    {
        // Check bounds
        if (x < 0 || x >= _gridWidth || y < 0 || y >= _gridHeight)
            return Task.FromResult(false);
        
        // Check if already occupied
        if (_serverOccupancyMap.IsOccupied(new CoreVector2I(x, y)))
            return Task.FromResult(false);
        
        return Task.FromResult(true);
    }

    public Task ApplyPlacementAsync(int x, int y, string structureType, int rotation)
    {
        var structureId = _nextStructureId++;
        _serverOccupancyMap.SetCellOccupancy(new CoreVector2I(x, y), true, 0, structureId);
        return Task.CompletedTask;
    }

    public Task<CellOccupancyData> GetCellOccupancyAsync(int x, int y)
    {
        var occupancy = _serverOccupancyMap.GetCellOccupancy(new CoreVector2I(x, y));
        return Task.FromResult(new CellOccupancyData
        {
            Occupied = occupancy.Occupied,
            EntityId = occupancy.EntityId,
            StructureId = occupancy.StructureId
        });
    }

    public Task<RegionOccupancyData> GetRegionOccupancyAsync(int x, int y, int width, int height)
    {
        var region = new RegionOccupancyData { Cells = new List<CellOccupancyData>() };
        
        for (int dy = 0; dy < height; dy++)
        {
            for (int dx = 0; dx < width; dx++)
            {
                var cellX = x + dx;
                var cellY = y + dy;
                if (cellX >= 0 && cellX < _gridWidth && cellY >= 0 && cellY < _gridHeight)
                {
                    var occupancy = _serverOccupancyMap.GetCellOccupancy(new CoreVector2I(cellX, cellY));
                    region.Cells.Add(new CellOccupancyData
                    {
                        Occupied = occupancy.Occupied,
                        EntityId = occupancy.EntityId,
                        StructureId = occupancy.StructureId
                    });
                }
            }
        }
        
        return Task.FromResult(region);
    }

    private void OnMessageReceived(object? sender, NetworkMessageEventArgs e)
    {
        if (e.Message is PlacementCommandMessage cmd)
        {
            Task.Run(async () => await HandlePlacementCommandAsync(e.PeerId, cmd));
        }
    }

    private async Task HandlePlacementCommandAsync(int peerId, PlacementCommandMessage cmd)
    {
        // Process through NetworkManager for authoritative handling
        var result = await _networkManager.ProcessPlacementCommandAsync(peerId, cmd);
        
        // Track placement change for replication
        var delta = new PlacementDeltaMessage
        {
            TickNumber = 0,
            Changes = new List<PlacementDeltaMessage.PlacementChange>
            {
                new() 
                { 
                    X = cmd.X, 
                    Y = cmd.Y, 
                    StructureType = cmd.StructureType,
                    Type = PlacementDeltaMessage.ChangeType.Added
                }
            }
        };
        
        _networkManager.ReplicationService.TrackPlacementChange(
            structureId: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            x: cmd.X,
            y: cmd.Y,
            type: PlacementDeltaMessage.ChangeType.Added,
            structureType: cmd.StructureType,
            rotation: cmd.Rotation
        );
        
        await _networkManager.ReplicationService.PublishPlacementDeltaAsync(delta);
        
        // Track occupancy change
        var occupancyDelta = new OccupancyDeltaMessage
        {
            TickNumber = 0,
            Changes = new List<OccupancyDeltaMessage.OccupancyChange>
            {
                new()
                {
                    X = cmd.X,
                    Y = cmd.Y,
                    Occupied = true,
                    StructureId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                }
            }
        };
        
        _networkManager.ReplicationService.TrackOccupancyChange(
            x: cmd.X,
            y: cmd.Y,
            occupied: true,
            structureId: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        );
        
        await _networkManager.ReplicationService.PublishOccupancyDeltaAsync(occupancyDelta);
    }

    public void Dispose()
    {
        Stop();
        _networkManager.DisconnectAsync().Wait();
        _networkManager.Dispose();
    }
}
