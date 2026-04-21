using MoonBark.NetworkSync.Core.Interfaces;
using MoonBark.NetworkSync.Core.Messages;
using MoonBark.NetworkSync.Core.Services;
using ServiceCellOccupancyData = MoonBark.NetworkSync.Core.Services.CellOccupancyData;
using MoonBark.NetworkSync.Tests.StressTests.Mocks;

namespace MoonBark.NetworkSync.Tests.StressTests.Infrastructure;

/// <summary>
/// Test server that handles multiple client connections for stress testing.
/// </summary>
public class StressTestServer : IDisposable
{
    private readonly NetworkManager _networkManager;
    private readonly TestOccupancyProvider _occupancyProvider;
    private readonly int _port;
    private readonly int _maxConnections;
    private bool _isRunning;

    public int ConnectedClientCount => GetConnectedPeerCount();

    public TestOccupancyProvider OccupancyProvider => _occupancyProvider;

    public event EventHandler<(int peerId, PlacementCommandMessage command)>? PlacementCommandReceived;

    public StressTestServer(int port = 7777, int maxConnections = 150)
    {
        _port = port;
        _maxConnections = maxConnections;
        _occupancyProvider = new TestOccupancyProvider(1000);

        _networkManager = NetworkManager.CreateServer(_occupancyProvider, port, maxConnections);
        _networkManager.PeerConnected += OnPeerConnected;
        _networkManager.PeerDisconnected += OnPeerDisconnected;
        _networkManager.Transport.MessageReceived += OnMessageReceived;
    }

    private int GetConnectedPeerCount()
    {
        lock (_peerLock)
        {
            return _connectedPeerIds.Count;
        }
    }

    private readonly HashSet<int> _connectedPeerIds = new();
    private readonly object _peerLock = new();

    public void Start()
    {
        if (_isRunning) return;
        _isRunning = true;
        Console.WriteLine($"[StressTestServer] Starting on port {_port} with max {_maxConnections} connections");
    }

    public void Stop()
    {
        if (!_isRunning) return;
        _isRunning = false;
        _networkManager.DisconnectAsync().Wait();
        Console.WriteLine("[StressTestServer] Stopped");
    }

    private void OnPeerConnected(object? sender, PeerConnectedEventArgs e)
    {
        lock (_peerLock)
        {
            _connectedPeerIds.Add(e.PeerId);
        }
        Console.WriteLine($"[StressTestServer] Client {e.PeerId} connected. Total: {_connectedPeerIds.Count}");
    }

    private void OnPeerDisconnected(object? sender, PeerDisconnectedEventArgs e)
    {
        lock (_peerLock)
        {
            _connectedPeerIds.Remove(e.PeerId);
        }
        Console.WriteLine($"[StressTestServer] Client {e.PeerId} disconnected. Total: {_connectedPeerIds.Count}");
    }

    private void OnMessageReceived(object? sender, NetworkMessageEventArgs e)
    {
        switch (e.Message)
        {
            case PlacementCommandMessage command:
                HandlePlacementCommand(e.PeerId, command);
                break;
        }
    }

    private async Task HandlePlacementCommand(int peerId, PlacementCommandMessage command)
    {
        PlacementCommandReceived?.Invoke(this, (peerId, command));

        // Process command through server authority
        var result = await _networkManager.ProcessPlacementCommandAsync(peerId, command);

        // Track placement changes for replication
        if (result.Success)
        {
            var delta = new PlacementDeltaMessage();
            _networkManager.ReplicationService.TrackPlacementChange(
                structureId: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                x: command.X,
                y: command.Y,
                type: PlacementDeltaChangeType.Added,
                structureType: command.StructureType,
                rotation: command.Rotation
            );
            await _networkManager.ReplicationService.PublishPlacementDeltaAsync(delta);

            // Publish occupancy delta
            var occupancyDelta = new OccupancyDeltaMessage();
            _networkManager.ReplicationService.TrackOccupancyChange(
                x: command.X,
                y: command.Y,
                occupied: true,
                structureId: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            );
            await _networkManager.ReplicationService.PublishOccupancyDeltaAsync(occupancyDelta);
        }
    }

    public int GetPlacementCount()
    {
        return _occupancyProvider.GetPlacementCount();
    }

    public Dictionary<(int x, int y), ServiceCellOccupancyData> GetAllPlacements()
    {
        return _occupancyProvider.GetAllPlacements();
    }

    public void Dispose()
    {
        Stop();
        _networkManager.Dispose();
    }
}

