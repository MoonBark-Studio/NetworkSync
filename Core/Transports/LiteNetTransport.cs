using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;
using MoonBark.NetworkSync.Core.Interfaces;
using MoonBark.Framework.Logging;
using MoonBark.NetworkSync.Core.Messages;

namespace MoonBark.NetworkSync.Core.Transports;

/// <summary>
/// LiteNetLib-based network transport implementation.
/// Provides UDP-based networking with reliable/unreliable messaging modes.
/// </summary>
public class LiteNetTransport : INetworkTransport, INetEventListener
{
    private const string DefaultGameVersion = "1.0.0";
    private const int DefaultUpdateTime = 15;
    private const int DefaultDisconnectTimeoutMs = 30000;
    private const int HandshakeTimeoutSeconds = 5;

    private NetManager? _netManager;
    private NetPeer? _serverPeer;
    private readonly Dictionary<int, NetPeer> _connectedPeers;
    private readonly Dictionary<byte, Func<INetworkMessage>> _messageFactories;
    private int _nextPeerId;
    private string _gameVersion = DefaultGameVersion;
    private TaskCompletionSource<bool>? _connectionCompletionSource;

    public bool IsConnected => _netManager != null && _netManager.IsRunning && ConnectionState == NetworkConnectionState.Connected;
    public bool IsServer { get; private set; }
    public NetworkConnectionState ConnectionState { get; private set; }

    public event EventHandler<NetworkMessageEventArgs>? MessageReceived;
    public event EventHandler<PeerConnectedEventArgs>? PeerConnected;
    public event EventHandler<PeerDisconnectedEventArgs>? PeerDisconnected;

    /// <summary>
    /// Gets or sets the game version for this transport instance.
    /// </summary>
    public string GameVersion
    {
        get => _gameVersion;
        set => _gameVersion = value ?? DefaultGameVersion;
    }

    private readonly IFrameworkLogger _logger;

    public LiteNetTransport() : this(new ConsoleFrameworkLogger("LiteNetTransport", FrameworkLogLevel.Debug)) { }

    public LiteNetTransport(IFrameworkLogger logger)
    {
        _logger = logger;
        _connectedPeers = new Dictionary<int, NetPeer>();
        _messageFactories = CreateMessageFactories();
        ConnectionState = NetworkConnectionState.Disconnected;
        _nextPeerId = 1;
    }

    private static Dictionary<byte, Func<INetworkMessage>> CreateMessageFactories()
    {
        return new Dictionary<byte, Func<INetworkMessage>>
        {
            [MessageTypes.PlacementCommand] = static () => new PlacementCommandMessage(),
            [MessageTypes.PlacementResult] = static () => new PlacementResultMessage(),
            [MessageTypes.PlacementDelta] = static () => new PlacementDeltaMessage(),
            [MessageTypes.OccupancyDelta] = static () => new OccupancyDeltaMessage(),
            [MessageTypes.CellOccupancy] = static () => new CellOccupancyMessage(),
            [MessageTypes.RegionOccupancy] = static () => new RegionOccupancyMessage(),
            [MessageTypes.WorldSnapshot] = static () => new WorldSnapshotMessage(),
            [MessageTypes.ChunkDelta] = static () => new ChunkDeltaMessage(),
            [MessageTypes.PredictedPlacement] = static () => new PredictedPlacementMessage(),
            [MessageTypes.PredictionReconcile] = static () => new PredictionReconcileMessage(),
            [MessageTypes.ConnectRequest] = static () => new ConnectRequestMessage(),
            [MessageTypes.ConnectResponse] = static () => new ConnectResponseMessage(),
            [MessageTypes.Disconnect] = static () => new DisconnectMessage(),
        };
    }

    public async Task StartServerAsync(int port, int maxConnections, CancellationToken cancellationToken = default)
    {
        IsServer = true;
        ConnectionState = NetworkConnectionState.Connecting;

        await Task.Run(() =>
        {
            _netManager = new NetManager(this)
            {
                AutoRecycle = true,
                UpdateTime = DefaultUpdateTime,
                DisconnectTimeout = DefaultDisconnectTimeoutMs,
                UseNativeSockets = true
            };

            if (_netManager.Start(port))
            {
                ConnectionState = NetworkConnectionState.Connected;
                _logger.Info($"Server started on port {port}");
            }
            else
            {
                ConnectionState = NetworkConnectionState.Disconnected;
                _logger.Error($"Failed to start server on port {port}");
            }
        }, cancellationToken);
    }

    public async Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
    {
        if (IsServer)
        {
            throw new InvalidOperationException("Cannot connect to server when running as server");
        }

        IsServer = false;
        ConnectionState = NetworkConnectionState.Connecting;
        _connectionCompletionSource = new TaskCompletionSource<bool>();

        await Task.Run(() =>
        {
            _netManager = new NetManager(this)
            {
                AutoRecycle = true,
                UpdateTime = DefaultUpdateTime,
                DisconnectTimeout = DefaultDisconnectTimeoutMs,
                UseNativeSockets = true
            };

            _netManager.Start();

            _serverPeer = _netManager.Connect(host, port, "ThistletideClient");
            _logger.Info($"Connecting to {host}:{port}...");
        }, cancellationToken);

        // Wait for connection to be established
        while (ConnectionState == NetworkConnectionState.Connecting && !cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(100, cancellationToken);
        }

        // Wait for version handshake to complete
        if (ConnectionState == NetworkConnectionState.Connected)
        {
            bool handshakeComplete = await _connectionCompletionSource.Task.WaitAsync(TimeSpan.FromSeconds(HandshakeTimeoutSeconds), cancellationToken);
            if (!handshakeComplete)
            {
                await DisconnectAsync();
                throw new InvalidOperationException("Connection handshake timed out");
            }
        }
    }

    public Task SendAsync(int peerId, INetworkMessage message, Interfaces.DeliveryMethod deliveryMethod)
    {
        if (!IsConnected)
        {
            _logger.Warning("Cannot send message: not connected");
            return Task.CompletedTask;
        }

        if (!_connectedPeers.TryGetValue(peerId, out var peer))
        {
            _logger.Warning($"Cannot send message: peer {peerId} not found");
            return Task.CompletedTask;
        }

        var liteNetDelivery = ConvertDeliveryMethod(deliveryMethod);
        var writer = new NetDataWriter();
        writer.Put(message.MessageType);
        writer.PutBytesWithLength(message.Serialize());
        peer.Send(writer, liteNetDelivery);

        return Task.CompletedTask;
    }

    public Task BroadcastAsync(INetworkMessage message, Interfaces.DeliveryMethod deliveryMethod, int? excludePeerId = null)
    {
        if (!IsConnected)
        {
            _logger.Warning("Cannot broadcast message: not connected");
            return Task.CompletedTask;
        }

        var liteNetDelivery = ConvertDeliveryMethod(deliveryMethod);
        byte[] payload = message.Serialize();

        foreach (var kvp in _connectedPeers)
        {
            if (excludePeerId.HasValue && kvp.Key == excludePeerId.Value)
            {
                continue;
            }

            var writer = new NetDataWriter();
            writer.Put(message.MessageType);
            writer.PutBytesWithLength(payload);
            kvp.Value.Send(writer, liteNetDelivery);
        }

        return Task.CompletedTask;
    }

    public async Task DisconnectAsync()
    {
        ConnectionState = NetworkConnectionState.Disconnecting;

        if (_netManager != null)
        {
            await Task.Run(() =>
            {
                if (IsServer)
                {
                    _netManager.Stop();
                    _connectedPeers.Clear();
                }
                else if (_serverPeer != null)
                {
                    _serverPeer.Disconnect();
                }

                _netManager.Stop();
                _logger.Info("Disconnected");
            });
        }

        ConnectionState = NetworkConnectionState.Disconnected;
    }

    #region INetEventListener Implementation

    public void OnPeerConnected(NetPeer peer)
    {
        int peerId;

        if (IsServer)
        {
            // Server assigns peer IDs
            peerId = _nextPeerId++;
            _connectedPeers[peerId] = peer;
            _logger.Info($"Client connected: {peer.EndPoint} (Peer ID: {peerId})");

            // Server waits for client's ConnectRequest message
            // Version validation will happen in OnNetworkReceive
        }
        else
        {
            // Client uses 0 for server
            peerId = 0;
            _serverPeer = peer;
            _connectedPeers[peerId] = peer;
            ConnectionState = NetworkConnectionState.Connected;
            _logger.Info($"Connected to server: {peer.EndPoint}");

            // Send ConnectRequest with game version
            var connectRequest = new ConnectRequestMessage
            {
                GameVersion = _gameVersion,
                ClientId = Guid.NewGuid().ToString()
            };
            SendAsync(0, connectRequest, Interfaces.DeliveryMethod.ReliableOrdered);
        }

        PeerConnected?.Invoke(this, new PeerConnectedEventArgs
        {
            PeerId = peerId,
            EndPoint = peer.EndPoint.ToString()
        });
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        int? peerId = _connectedPeers.FirstOrDefault(kvp => kvp.Value == peer).Key;

        if (peerId.HasValue)
        {
            _connectedPeers.Remove(peerId.Value);
            _logger.Info($"Peer {peerId.Value} disconnected: {disconnectInfo.Reason}");

            PeerDisconnected?.Invoke(this, new PeerDisconnectedEventArgs
            {
                PeerId = peerId.Value,
                Reason = ConvertDisconnectReason(disconnectInfo.Reason)
            });
        }

        if (!IsServer && peer == _serverPeer)
        {
            ConnectionState = NetworkConnectionState.Disconnected;
        }
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, LiteNetLib.DeliveryMethod deliveryMethod)
    {
        byte messageType = reader.GetByte();
        byte[] payload = reader.GetBytesWithLength();

        if (_messageFactories.TryGetValue(messageType, out Func<INetworkMessage>? factory))
        {
            INetworkMessage message = factory();
            message.Deserialize(payload);

            // Handle connection handshake messages
            if (message is ConnectRequestMessage connectRequest)
            {
                HandleConnectRequest(peer, connectRequest);
                reader.Recycle();
                return;
            }

            if (message is ConnectResponseMessage connectResponse)
            {
                HandleConnectResponse(connectResponse);
                reader.Recycle();
                return;
            }

            DispatchMessage(peer, message);
        }

        reader.Recycle();
    }

    private void HandleConnectRequest(NetPeer peer, ConnectRequestMessage request)
    {
        if (!IsServer)
        {
            _logger.Debug("Received ConnectRequest on client - ignoring");
            return;
        }

        int peerId = _connectedPeers.FirstOrDefault(kvp => kvp.Value == peer).Key;
        _logger.Info($"Received ConnectRequest from peer {peerId}: version {request.GameVersion}");

        var response = new ConnectResponseMessage
        {
            ServerGameVersion = _gameVersion,
            AssignedPeerId = peerId
        };

        // Validate version
        if (request.GameVersion != _gameVersion)
        {
            response.Accepted = false;
            response.RejectReason = $"Game version mismatch: client version {request.GameVersion} does not match server version {_gameVersion}";
            _logger.Warning($"Rejecting connection from peer {peerId}: {response.RejectReason}");
        }
        else
        {
            response.Accepted = true;
            _logger.Info($"Accepting connection from peer {peerId}: version {request.GameVersion}");
        }

        // Send response
        SendAsync(peerId, response, Interfaces.DeliveryMethod.ReliableOrdered);

        // If rejected, disconnect the peer
        if (!response.Accepted)
        {
            Task.Run(async () =>
            {
                await Task.Delay(100); // Give time for response to be sent
                peer.Disconnect();
            });
        }
    }

    private void HandleConnectResponse(ConnectResponseMessage response)
    {
        if (IsServer)
        {
            _logger.Debug("Received ConnectResponse on server - ignoring");
            return;
        }

        _logger.Info($"Received ConnectResponse: accepted={response.Accepted}, reason={response.RejectReason}");

        if (response.Accepted)
        {
            _connectionCompletionSource?.SetResult(true);
        }
        else
        {
            _connectionCompletionSource?.SetException(new InvalidOperationException(
                $"Connection rejected by server: {response.RejectReason} (Server version: {response.ServerGameVersion}, Client version: {_gameVersion})"));
            ConnectionState = NetworkConnectionState.Disconnected;
        }
    }

    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {
        // Handle unconnected messages if needed
    }

    public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
        // Track latency if needed
    }

    public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
    {
        _logger.Error($"Network error: {endPoint} - {socketError}");
    }

    public void OnConnectionRequest(ConnectionRequest request)
    {
        if (IsServer)
        {
            request.AcceptIfKey("ThistletideClient");
        }
    }

    #endregion

    #region Helper Methods

    private void DispatchMessage(NetPeer peer, INetworkMessage message)
    {
        int peerId = _connectedPeers.FirstOrDefault(kvp => kvp.Value == peer).Key;
        MessageReceived?.Invoke(this, new NetworkMessageEventArgs { PeerId = peerId, Message = message });
    }

    private LiteNetLib.DeliveryMethod ConvertDeliveryMethod(Interfaces.DeliveryMethod method)
    {
        return method switch
        {
            Interfaces.DeliveryMethod.ReliableOrdered => LiteNetLib.DeliveryMethod.ReliableOrdered,
            Interfaces.DeliveryMethod.ReliableUnordered => LiteNetLib.DeliveryMethod.ReliableUnordered,
            Interfaces.DeliveryMethod.Unreliable => LiteNetLib.DeliveryMethod.Unreliable,
            Interfaces.DeliveryMethod.UnreliableSequenced => LiteNetLib.DeliveryMethod.Sequenced,
            _ => LiteNetLib.DeliveryMethod.ReliableOrdered
        };
    }

    private Interfaces.DisconnectReason ConvertDisconnectReason(LiteNetLib.DisconnectReason disconnectReason)
    {
        return disconnectReason switch
        {
            LiteNetLib.DisconnectReason.ConnectionFailed => Interfaces.DisconnectReason.ConnectionLost,
            LiteNetLib.DisconnectReason.RemoteConnectionClose => Interfaces.DisconnectReason.PeerInitiatedDisconnect,
            LiteNetLib.DisconnectReason.Timeout => Interfaces.DisconnectReason.Timeout,
            LiteNetLib.DisconnectReason.HostUnreachable => Interfaces.DisconnectReason.ConnectionLost,
            LiteNetLib.DisconnectReason.NetworkUnreachable => Interfaces.DisconnectReason.ConnectionLost,
            LiteNetLib.DisconnectReason.Reconnect => Interfaces.DisconnectReason.ConnectionLost,
            _ => Interfaces.DisconnectReason.DisconnectCalled
        };
    }

    #endregion
}
