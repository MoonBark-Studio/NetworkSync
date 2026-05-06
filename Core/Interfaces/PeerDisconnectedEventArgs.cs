namespace MoonBark.NetworkSync.Core.Interfaces;

/// <summary>
/// Event arguments for peer disconnected events.
/// </summary>
public class PeerDisconnectedEventArgs : EventArgs
{
    public int PeerId { get; set; }
    public DisconnectReason Reason { get; set; }
}
