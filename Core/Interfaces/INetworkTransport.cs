namespace MoonBark.NetworkSync.Core.Interfaces;

/// <summary>
/// Defines the contract for network transport implementations.
/// Provides abstraction over low-level networking details.
/// </summary>
public interface INetworkTransport
{
    /// <summary>
    /// Gets whether the transport is currently connected.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Gets whether this instance is running as a server.
    /// </summary>
    bool IsServer { get; }

    /// <summary>
    /// Gets the current connection state.
    /// </summary>
    NetworkConnectionState ConnectionState { get; }

    /// <summary>
    /// Event raised when a message is received from a peer.
    /// </summary>
    event EventHandler<NetworkMessageEventArgs> MessageReceived;

    /// <summary>
    /// Event raised when a peer connects.
    /// </summary>
    event EventHandler<PeerConnectedEventArgs> PeerConnected;

    /// <summary>
    /// Event raised when a peer disconnects.
    /// </summary>
    event EventHandler<PeerDisconnectedEventArgs> PeerDisconnected;

    /// <summary>
    /// Starts the server listening on the specified port.
    /// </summary>
    /// <param name="port">The port to listen on.</param>
    /// <param name="maxConnections">Maximum number of connections.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StartServerAsync(int port, int maxConnections, CancellationToken cancellationToken = default);

    /// <summary>
    /// Connects to a server at the specified address.
    /// </summary>
    /// <param name="host">The server host address.</param>
    /// <param name="port">The server port.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a message to a specific peer.
    /// </summary>
    /// <param name="peerId">The peer ID to send to.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="deliveryMethod">The delivery method (reliable/unreliable).</param>
    Task SendAsync(int peerId, INetworkMessage message, DeliveryMethod deliveryMethod);

    /// <summary>
    /// Sends a message to all connected peers.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="deliveryMethod">The delivery method (reliable/unreliable).</param>
    /// <param name="excludePeerId">Optional peer ID to exclude from broadcast.</param>
    Task BroadcastAsync(INetworkMessage message, DeliveryMethod deliveryMethod, int? excludePeerId = null);

    /// <summary>
    /// Disconnects from the server or stops the server.
    /// </summary>
    Task DisconnectAsync();
}

/// <summary>
/// Network connection states.
/// </summary>
public enum NetworkConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Disconnecting
}

/// <summary>
/// Delivery method for network messages.
/// </summary>
public enum DeliveryMethod
{
    /// <summary>
    /// Reliable ordered delivery (TCP-like).
    /// </summary>
    ReliableOrdered,

    /// <summary>
    /// Reliable unordered delivery.
    /// </summary>
    ReliableUnordered,

    /// <summary>
    /// Unreliable delivery (fire and forget).
    /// </summary>
    Unreliable,

    /// <summary>
    /// Unreliable sequenced delivery.
    /// </summary>
    UnreliableSequenced
}

/// <summary>
/// Event arguments for network message received events.
/// </summary>
public class NetworkMessageEventArgs : EventArgs
{
    public int PeerId { get; set; }
    public INetworkMessage Message { get; set; } = null!;
}

/// <summary>
/// Event arguments for peer connected events.
/// </summary>
public class PeerConnectedEventArgs : EventArgs
{
    public int PeerId { get; set; }
    public string EndPoint { get; set; } = string.Empty;
}

/// <summary>
/// Event arguments for peer disconnected events.
/// </summary>
public class PeerDisconnectedEventArgs : EventArgs
{
    public int PeerId { get; set; }
    public DisconnectReason Reason { get; set; }
}

/// <summary>
/// Reasons for peer disconnection.
/// </summary>
public enum DisconnectReason
{
    ConnectionLost,
    DisconnectCalled,
    Timeout,
    PeerInitiatedDisconnect,
    ServerShutdown
}
