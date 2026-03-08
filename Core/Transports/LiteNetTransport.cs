using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;
using NetworkSync.Core.Interfaces;
using NetworkSync.Core.Messages;

namespace NetworkSync.Core.Transports;

/// <summary>
/// LiteNetLib-based network transport implementation.
/// Provides UDP-based networking with reliable/unreliable messaging modes.
/// </summary>
public class LiteNetTransport : INetworkTransport, INetEventListener
{
    private NetManager? _netManager;
    private NetPeer? _serverPeer;
    private readonly Dictionary<int, NetPeer> _connectedPeers;
    private readonly Dictionary<byte, Func<INetworkMessage>> _messageFactories;
    private int _nextPeerId;

    public bool IsConnected => _netManager != null && _netManager.IsRunning && ConnectionState == NetworkConnectionState.Connected;
    public bool IsServer { get; private set; }
    public NetworkConnectionState ConnectionState { get; private set; }

    public event EventHandler<NetworkMessageEventArgs>? MessageReceived;
    public event EventHandler<PeerConnectedEventArgs>? PeerConnected;
    public event EventHandler<PeerDisconnectedEventArgs>? PeerDisconnected;

    public LiteNetTransport()
    {
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
                UpdateTime = 15,
                DisconnectTimeout = 30000,
                UseNativeSockets = true
            };

            if (_netManager.Start(port))
            {
                ConnectionState = NetworkConnectionState.Connected;
                Console.WriteLine($"[LiteNetTransport] Server started on port {port}");
            }
            else
            {
                ConnectionState = NetworkConnectionState.Disconnected;
                Console.WriteLine($"[LiteNetTransport] Failed to start server on port {port}");
            }
        }, cancellationToken);
    }

    public async Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
    {
        IsServer = false;
        ConnectionState = NetworkConnectionState.Connecting;

        await Task.Run(() =>
        {
            _netManager = new NetManager(this)
            {
                AutoRecycle = true,
                UpdateTime = 15,
                DisconnectTimeout = 30000,
                UseNativeSockets = true
            };

            _netManager.Start();

            _serverPeer = _netManager.Connect(host, port, "ThistletideClient");
            Console.WriteLine($"[LiteNetTransport] Connecting to {host}:{port}...");
        }, cancellationToken);

        // Wait for connection to be established
        while (ConnectionState == NetworkConnectionState.Connecting && !cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(100, cancellationToken);
        }
    }

    public Task SendAsync(int peerId, INetworkMessage message, Interfaces.DeliveryMethod deliveryMethod)
    {
        if (!IsConnected)
        {
            Console.WriteLine($"[LiteNetTransport] Cannot send message: not connected");
            return Task.CompletedTask;
        }

        if (!_connectedPeers.TryGetValue(peerId, out var peer))
        {
            Console.WriteLine($"[LiteNetTransport] Cannot send message: peer {peerId} not found");
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
            Console.WriteLine($"[LiteNetTransport] Cannot broadcast message: not connected");
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
                Console.WriteLine("[LiteNetTransport] Disconnected");
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
            Console.WriteLine($"[LiteNetTransport] Client connected: {peer.EndPoint} (Peer ID: {peerId})");
        }
        else
        {
            // Client uses 0 for server
            peerId = 0;
            _serverPeer = peer;
            _connectedPeers[peerId] = peer;
            ConnectionState = NetworkConnectionState.Connected;
            Console.WriteLine($"[LiteNetTransport] Connected to server: {peer.EndPoint}");
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
            Console.WriteLine($"[LiteNetTransport] Peer {peerId.Value} disconnected: {disconnectInfo.Reason}");

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
            DispatchMessage(peer, message);
        }

        reader.Recycle();
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
        Console.WriteLine($"[LiteNetTransport] Network error: {endPoint} - {socketError}");
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
