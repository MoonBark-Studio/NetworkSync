using MoonBark.NetworkSync.Core.Interfaces;
using MoonBark.NetworkSync.Core.Messages;
using MoonBark.NetworkSync.Core.Services;
using MoonBark.Framework.Logging;
using MoonBark.NetworkSync.Core.Transports;

namespace MoonBark.NetworkSync.Core.Services;

/// <summary>
/// Main network manager that coordinates all networking services.
/// Provides a unified interface for both server and client scenarios.
/// </summary>
public sealed class NetworkManager : IDisposable
{
    private const int TargetFrameTimeMs = 16;

    private readonly LiteNetTransport _transport;
    private readonly ReplicationService _replicationService;
    private readonly ServerAuthorityService? _serverAuthority;
    private readonly ClientPredictionService? _clientPrediction;
    private readonly IFrameworkLogger _logger;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private Task? _updateLoop;

    /// <summary>
    /// Gets the network transport.
    /// </summary>
    public INetworkTransport Transport => _transport;

    /// <summary>
    /// Gets the replication service.
    /// </summary>
    public IReplicationService ReplicationService => _replicationService;

    /// <summary>
    /// Gets the server authority service (server only).
    /// </summary>
    public IServerAuthorityService? ServerAuthority => _serverAuthority;

    /// <summary>
    /// Gets the client prediction service (client only).
    /// </summary>
    public IClientPredictionService? ClientPrediction => _clientPrediction;

    /// <summary>
    /// Gets the concrete replication service for advanced host-side integration.
    /// </summary>
    public ReplicationService Replication => _replicationService;

    /// <summary>
    /// Gets the concrete client prediction service for advanced client-side integration.
    /// </summary>
    public ClientPredictionService? ClientPredictionService => _clientPrediction;

    /// <summary>
    /// Gets whether this instance is running as a server.
    /// </summary>
    public bool IsServer => _transport.IsServer;

    /// <summary>
    /// Gets whether the network is connected.
    /// </summary>
    public bool IsConnected => _transport.IsConnected;

    /// <summary>
    /// Event raised when a peer connects.
    /// </summary>
    public event EventHandler<PeerConnectedEventArgs>? PeerConnected;

    /// <summary>
    /// Event raised when a peer disconnects.
    /// </summary>
    public event EventHandler<PeerDisconnectedEventArgs>? PeerDisconnected;

    /// <summary>
    /// Creates a server-side network manager.
    /// </summary>
    public static NetworkManager CreateServer(ICoreOccupancyProvider occupancyProvider, int port = 7777, int maxConnections = 10)
    {
        var transport = new LiteNetTransport();
        var replicationService = new ReplicationService(transport);
        var serverAuthority = new ServerAuthorityService(occupancyProvider);

        var manager = new NetworkManager(transport, replicationService, serverAuthority, null);
        manager.InitializeServer(port, maxConnections);

        return manager;
    }

    /// <summary>
    /// Creates a client-side network manager.
    /// </summary>
    public static NetworkManager CreateClient(ILocalOccupancyValidator localValidator)
    {
        var transport = new LiteNetTransport();
        var replicationService = new ReplicationService(transport);
        var clientPrediction = new ClientPredictionService(localValidator);

        var manager = new NetworkManager(transport, replicationService, null, clientPrediction);

        return manager;
    }

    private NetworkManager(
        LiteNetTransport transport,
        ReplicationService replicationService,
        ServerAuthorityService? serverAuthority,
        ClientPredictionService? clientPrediction)
    {
        _transport = transport;
        _replicationService = replicationService;
        _serverAuthority = serverAuthority;
        _clientPrediction = clientPrediction;
        _logger = new ConsoleFrameworkLogger("NetworkManager", FrameworkLogLevel.Debug);
        _cancellationTokenSource = new CancellationTokenSource();

        // Subscribe to transport events
        _transport.PeerConnected += OnPeerConnected;
        _transport.PeerDisconnected += OnPeerDisconnected;
    }

    private void InitializeServer(int port, int maxConnections)
    {
        Task.Run(async () =>
        {
            await _transport.StartServerAsync(port, maxConnections, _cancellationTokenSource.Token);
            StartUpdateLoop();
        });
    }

    /// <summary>
    /// Connects to a server (client only).
    /// </summary>
    public async Task ConnectAsync(string host, int port = 7777)
    {
        if (IsServer)
        {
            throw new InvalidOperationException("Cannot connect to server when running as server");
        }

        await _transport.ConnectAsync(host, port, _cancellationTokenSource.Token);
        StartUpdateLoop();
    }

    /// <summary>
    /// Sends a placement command to the server (client only).
    /// </summary>
    public async Task<PlacementResultMessage> SendPlacementCommandAsync(PlacementCommandMessage command)
    {
        if (IsServer)
        {
            throw new InvalidOperationException("Cannot send command to server when running as server");
        }

        // Predict locally first
        var predictedResult = _clientPrediction!.PredictPlacement(command);

        // Send command to server
        await _transport.SendAsync(0, command, DeliveryMethod.ReliableOrdered);

        return predictedResult;
    }

    /// <summary>
    /// Processes a placement command from a client (server only).
    /// </summary>
    public async Task<PlacementResultMessage> ProcessPlacementCommandAsync(int peerId, PlacementCommandMessage command)
    {
        if (!IsServer)
        {
            throw new InvalidOperationException("Cannot process client command when running as client");
        }

        // Validate and apply
        var result = await _serverAuthority!.ApplyPlacementCommandAsync(command);

        // Send result back to client
        await _transport.SendAsync(peerId, result, DeliveryMethod.ReliableOrdered);

        return result;
    }

    /// <summary>
    /// Disconnects from the network.
    /// </summary>
    public async Task DisconnectAsync()
    {
        _cancellationTokenSource.Cancel();

        if (_updateLoop != null)
        {
            await _updateLoop;
        }

        await _transport.DisconnectAsync();
    }

    private void StartUpdateLoop()
    {
        _updateLoop = Task.Run(async () =>
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                await UpdateAsync(_cancellationTokenSource.Token);
                await Task.Delay(TargetFrameTimeMs, _cancellationTokenSource.Token);
            }
        });
    }

    private async Task UpdateAsync(CancellationToken cancellationToken)
    {
        // Process network events
        // LiteNetLib handles this internally, but we can add custom logic here

        // Server: publish deltas periodically
        if (IsServer)
        {
            // In a real implementation, this would check for changes and publish deltas
            // For now, this is a placeholder
        }

        await Task.CompletedTask;
    }

    private void OnPeerConnected(object? sender, PeerConnectedEventArgs e)
    {
        _logger.Info($"Peer {e.PeerId} connected from {e.EndPoint}");
        PeerConnected?.Invoke(this, e);
    }

    private void OnPeerDisconnected(object? sender, PeerDisconnectedEventArgs e)
    {
        _logger.Info($"Peer {e.PeerId} disconnected: {e.Reason}");
        PeerDisconnected?.Invoke(this, e);
    }

    /// <summary>
    /// Disposes the manager and releases network resources.
    /// </summary>
    public void Dispose()
    {
        _cancellationTokenSource.Cancel();

        try
        {
            if (_updateLoop != null)
            {
                _updateLoop.GetAwaiter().GetResult();
            }
        }
        catch (OperationCanceledException)
        {
        }

        _transport.PeerConnected -= OnPeerConnected;
        _transport.PeerDisconnected -= OnPeerDisconnected;

        _transport.DisconnectAsync().GetAwaiter().GetResult();
        _cancellationTokenSource.Dispose();
    }
}
