using MoonBark.NetworkSync.Core.Interfaces;

namespace MoonBark.NetworkSync.Tests.StressTests.Mocks;

/// <summary>
/// In-memory transport double for deterministic replication tests.
/// </summary>
public sealed class MockNetworkTransport : INetworkTransport
{
    private readonly List<(int peerId, INetworkMessage message, DeliveryMethod method)> _sentMessages;
    private readonly List<(INetworkMessage message, DeliveryMethod method, int? excludePeerId)> _broadcastMessages;

    public MockNetworkTransport()
    {
        _sentMessages = new List<(int peerId, INetworkMessage message, DeliveryMethod method)>();
        _broadcastMessages = new List<(INetworkMessage message, DeliveryMethod method, int? excludePeerId)>();
        ConnectionState = NetworkConnectionState.Disconnected;
    }

    public bool IsConnected { get; private set; }

    public bool IsServer { get; private set; }

    public NetworkConnectionState ConnectionState { get; private set; }

    public event EventHandler<NetworkMessageEventArgs>? MessageReceived;

    public event EventHandler<PeerConnectedEventArgs>? PeerConnected;

    public event EventHandler<PeerDisconnectedEventArgs>? PeerDisconnected;

    public IReadOnlyList<(int peerId, INetworkMessage message, DeliveryMethod method)> SentMessages => _sentMessages;

    public IReadOnlyList<(INetworkMessage message, DeliveryMethod method, int? excludePeerId)> BroadcastMessages => _broadcastMessages;

    public Task StartServerAsync(int port, int maxConnections, CancellationToken cancellationToken = default)
    {
        IsServer = true;
        IsConnected = true;
        ConnectionState = NetworkConnectionState.Connected;
        return Task.CompletedTask;
    }

    public Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
    {
        IsServer = false;
        IsConnected = true;
        ConnectionState = NetworkConnectionState.Connected;
        return Task.CompletedTask;
    }

    public Task SendAsync(int peerId, INetworkMessage message, DeliveryMethod deliveryMethod)
    {
        _sentMessages.Add((peerId, message, deliveryMethod));
        return Task.CompletedTask;
    }

    public Task BroadcastAsync(INetworkMessage message, DeliveryMethod deliveryMethod, int? excludePeerId = null)
    {
        _broadcastMessages.Add((message, deliveryMethod, excludePeerId));
        return Task.CompletedTask;
    }

    public Task DisconnectAsync()
    {
        IsConnected = false;
        ConnectionState = NetworkConnectionState.Disconnected;
        return Task.CompletedTask;
    }

    public void RaiseMessageReceived(int peerId, INetworkMessage message)
    {
        MessageReceived?.Invoke(this, new NetworkMessageEventArgs { PeerId = peerId, Message = message });
    }

    public void RaisePeerConnected(int peerId, string endPoint)
    {
        PeerConnected?.Invoke(this, new PeerConnectedEventArgs { PeerId = peerId, EndPoint = endPoint });
    }

    public void RaisePeerDisconnected(int peerId, DisconnectReason reason)
    {
        PeerDisconnected?.Invoke(this, new PeerDisconnectedEventArgs { PeerId = peerId, Reason = reason });
    }
}
