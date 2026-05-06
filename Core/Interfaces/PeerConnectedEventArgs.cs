namespace MoonBark.NetworkSync.Core.Interfaces;

/// <summary>
/// Event arguments for peer connected events.
/// </summary>
public class PeerConnectedEventArgs : EventArgs
{
    public int PeerId { get; set; }
    public string EndPoint { get; set; } = string.Empty;
}
