using MoonBark.NetworkSync.Core.Interfaces;

namespace MoonBark.NetworkSync.Core.Messages;

/// <summary>
/// Connection handshake request from client to server.
/// </summary>
public class ConnectRequestMessage : NetworkMessageBase
{
    public override byte MessageType => MessageTypes.ConnectRequest;

    /// <summary>
    /// The game version of the client.
    /// </summary>
    public string GameVersion { get; set; } = string.Empty;

    /// <summary>
    /// The client's unique identifier.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;
}

/// <summary>
/// Connection handshake response from server to client.
/// </summary>
public class ConnectResponseMessage : NetworkMessageBase
{
    public override byte MessageType => MessageTypes.ConnectResponse;

    /// <summary>
    /// Whether the connection was accepted.
    /// </summary>
    public bool Accepted { get; set; }

    /// <summary>
    /// The reason for rejection if not accepted.
    /// </summary>
    public string RejectReason { get; set; } = string.Empty;

    /// <summary>
    /// The assigned peer ID for this client.
    /// </summary>
    public int AssignedPeerId { get; set; }

    /// <summary>
    /// The server's game version.
    /// </summary>
    public string ServerGameVersion { get; set; } = string.Empty;
}

/// <summary>
/// Disconnect message sent when a peer disconnects gracefully.
/// </summary>
public class DisconnectMessage : NetworkMessageBase
{
    public override byte MessageType => MessageTypes.Disconnect;

    /// <summary>
    /// The reason for disconnection.
    /// </summary>
    public string Reason { get; set; } = string.Empty;
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
