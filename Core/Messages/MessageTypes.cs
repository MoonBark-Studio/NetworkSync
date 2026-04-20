namespace MoonBark.NetworkSync.Core.Messages;

/// <summary>
/// Message type identifiers for network serialization.
/// </summary>
public static class MessageTypes
{
    // Placement messages (1-10)
    public const byte PlacementCommand = 1;
    public const byte PlacementResult = 2;
    public const byte PlacementDelta = 3;
    public const byte PlacementRemoved = 4;

    // Occupancy messages (11-20)
    public const byte OccupancyDelta = 11;
    public const byte CellOccupancy = 12;
    public const byte RegionOccupancy = 13;

    // World state messages (21-30)
    public const byte WorldSnapshot = 21;
    public const byte ChunkDelta = 22;

    // Prediction messages (31-40)
    public const byte PredictedPlacement = 31;
    public const byte PredictionReconcile = 32;

    // Connection messages (41-50)
    public const byte ConnectRequest = 41;
    public const byte ConnectResponse = 42;
    public const byte Disconnect = 43;
}