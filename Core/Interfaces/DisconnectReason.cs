namespace MoonBark.NetworkSync.Core.Interfaces;

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
