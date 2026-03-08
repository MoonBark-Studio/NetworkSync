using NetworkSync.Core.Interfaces;
using NetworkSync.Core.Messages;
using NetworkSync.Core.Services;
using NetworkSync.Tests.StressTests.Mocks;

namespace NetworkSync.Tests.StressTests.Infrastructure;

/// <summary>
/// Test client for stress testing that can connect to server and send placement commands.
/// </summary>
public class StressTestClient : IDisposable
{
    private readonly NetworkManager _networkManager;
    private readonly TestLocalValidator _localValidator;
    private readonly string _host;
    private readonly int _port;
    private long _nextCommandId;
    private readonly object _commandLock = new();

    public bool IsConnected => _networkManager.IsConnected;
    public int ClientId { get; private set; }
    public TestLocalValidator LocalValidator => _localValidator;

    public event EventHandler<PlacementResultMessage>? PlacementResultReceived;
    public event EventHandler<PlacementDeltaMessage>? PlacementDeltaReceived;
    public event EventHandler<OccupancyDeltaMessage>? OccupancyDeltaReceived;

    public StressTestClient(string host = "127.0.0.1", int port = 7777)
    {
        _host = host;
        _port = port;
        _localValidator = new TestLocalValidator(1000);
        _nextCommandId = 1;

        _networkManager = NetworkManager.CreateClient(_localValidator);
        _networkManager.Transport.MessageReceived += OnMessageReceived;
        _networkManager.ReplicationService.PlacementDeltaReceived += OnReplicationPlacementDelta;
        _networkManager.ReplicationService.OccupancyDeltaReceived += OnReplicationOccupancyDelta;
    }

    public async Task<bool> ConnectAsync(int clientId, int timeoutMs = 10000)
    {
        ClientId = clientId;

        try
        {
            await _networkManager.ConnectAsync(_host, _port);
            return IsConnected;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[StressTestClient {ClientId}] Connection failed: {ex.Message}");
            return false;
        }
    }

    public async Task DisconnectAsync()
    {
        if (IsConnected)
        {
            await _networkManager.DisconnectAsync();
        }
    }

    public async Task<(bool success, long latencyMs)> SendPlacementCommandAsync(int x, int y, string structureType = "TestStructure", int rotation = 0)
    {
        if (!IsConnected)
        {
            return (false, 0);
        }

        long commandId;
        lock (_commandLock)
        {
            commandId = _nextCommandId++;
        }

        var command = new PlacementCommandMessage
        {
            CommandId = commandId,
            ClientId = ClientId,
            X = x,
            Y = y,
            StructureType = structureType,
            Rotation = rotation,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        var sendTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        try
        {
            // Send command (predicts locally first)
            await _networkManager.SendPlacementCommandAsync(command);

            // For stress testing, we consider the command "sent" when we get the prediction
            // In real scenario, we'd wait for server response
            var latency = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - sendTime;

            return (true, latency);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[StressTestClient {ClientId}] Send failed: {ex.Message}");
            return (false, 0);
        }
    }

    public void UpdateLocalOccupancy(int x, int y, bool occupied, long? entityId = null, long? structureId = null)
    {
        _localValidator.UpdateLocalOccupancy(x, y, occupied, entityId, structureId);
    }

    private void OnMessageReceived(object? sender, NetworkMessageEventArgs e)
    {
        switch (e.Message)
        {
            case PlacementResultMessage result:
                PlacementResultReceived?.Invoke(this, result);
                break;
        }
    }

    private void OnReplicationPlacementDelta(object? sender, PlacementDeltaReceivedEventArgs e)
    {
        foreach (var change in e.Delta.Changes)
        {
            if (change.Type == PlacementDeltaMessage.ChangeType.Added)
            {
                _localValidator.UpdateLocalOccupancy(change.X, change.Y, true, structureId: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            }
            else if (change.Type == PlacementDeltaMessage.ChangeType.Removed)
            {
                _localValidator.UpdateLocalOccupancy(change.X, change.Y, false);
            }
        }

        PlacementDeltaReceived?.Invoke(this, e.Delta);
    }

    private void OnReplicationOccupancyDelta(object? sender, OccupancyDeltaReceivedEventArgs e)
    {
        foreach (var change in e.Delta.Changes)
        {
            _localValidator.UpdateLocalOccupancy(change.X, change.Y, change.Occupied, change.EntityId, change.StructureId);
        }

        OccupancyDeltaReceived?.Invoke(this, e.Delta);
    }

    public int GetLocalPlacementCount()
    {
        return _localValidator.GetLocalPlacementCount();
    }

    public Dictionary<(int x, int y), CellOccupancyData> GetLocalPlacements()
    {
        return _localValidator.GetLocalOccupancy();
    }

    public void Dispose()
    {
        DisconnectAsync().Wait();
        _networkManager.Dispose();
    }
}
