using NetworkSync.Core.Interfaces;

namespace NetworkSync.Core.Messages;

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

/// <summary>
/// Represents a placement command from a client to the server.
/// </summary>
public class PlacementCommandMessage : NetworkMessageBase
{
    public override byte MessageType => MessageTypes.PlacementCommand;

    /// <summary>
    /// The unique identifier for the placement command.
    /// </summary>
    public long CommandId { get; set; }

    /// <summary>
    /// The client ID issuing the command.
    /// </summary>
    public int ClientId { get; set; }

    /// <summary>
    /// The X coordinate of the placement.
    /// </summary>
    public int X { get; set; }

    /// <summary>
    /// The Y coordinate of the placement.
    /// </summary>
    public int Y { get; set; }

    /// <summary>
    /// The structure type to place.
    /// </summary>
    public string StructureType { get; set; } = string.Empty;

    /// <summary>
    /// The rotation of the structure.
    /// </summary>
    public int Rotation { get; set; }

    /// <summary>
    /// The timestamp when the command was issued.
    /// </summary>
    public long Timestamp { get; set; }
}

/// <summary>
/// Represents the result of a placement command from server to client.
/// </summary>
public class PlacementResultMessage : NetworkMessageBase
{
    public override byte MessageType => MessageTypes.PlacementResult;

    /// <summary>
    /// The command ID this result corresponds to.
    /// </summary>
    public long CommandId { get; set; }

    /// <summary>
    /// Whether the placement was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The reason for failure if unsuccessful.
    /// </summary>
    public string FailureReason { get; set; } = string.Empty;

    /// <summary>
    /// The server timestamp when the placement was processed.
    /// </summary>
    public long ServerTimestamp { get; set; }
}
