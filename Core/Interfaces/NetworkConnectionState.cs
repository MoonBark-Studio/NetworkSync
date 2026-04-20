namespace MoonBark.NetworkSync.Core.Interfaces;

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