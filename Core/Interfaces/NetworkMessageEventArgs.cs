namespace MoonBark.NetworkSync.Core.Interfaces;

/// <summary>
/// Event arguments for network message received events.
/// </summary>
public class NetworkMessageEventArgs : EventArgs
{
    public int PeerId { get; set; }
    public INetworkMessage Message { get; set; } = null!;
}
