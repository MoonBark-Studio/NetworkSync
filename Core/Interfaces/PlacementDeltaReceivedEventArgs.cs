namespace MoonBark.NetworkSync.Core.Interfaces;

/// <summary>
/// Event arguments for placement delta received events.
/// </summary>
public class PlacementDeltaReceivedEventArgs : EventArgs
{
    public Messages.PlacementDeltaMessage Delta { get; set; } = null!;
}