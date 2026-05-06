namespace MoonBark.NetworkSync.Core.Interfaces;

/// <summary>
/// Event arguments for world snapshot received events.
/// </summary>
public class WorldSnapshotReceivedEventArgs : EventArgs
{
    public Messages.WorldSnapshotMessage Snapshot { get; set; } = null!;
}
