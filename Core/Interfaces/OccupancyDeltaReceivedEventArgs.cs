namespace MoonBark.NetworkSync.Core.Interfaces;

/// <summary>
/// Event arguments for occupancy delta received events.
/// </summary>
public class OccupancyDeltaReceivedEventArgs : EventArgs
{
    public Messages.OccupancyDeltaMessage Delta { get; set; } = null!;
}