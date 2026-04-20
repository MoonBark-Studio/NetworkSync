namespace MoonBark.NetworkSync.Core.Interfaces;

/// <summary>
/// Delivery method for network messages.
/// </summary>
public enum DeliveryMethod
{
    /// <summary>
    /// Reliable ordered delivery (TCP-like).
    /// </summary>
    ReliableOrdered,

    /// <summary>
    /// Reliable unordered delivery.
    /// </summary>
    ReliableUnordered,

    /// <summary>
    /// Unreliable delivery (fire and forget).
    /// </summary>
    Unreliable,

    /// <summary>
    /// Unreliable sequenced delivery.
    /// </summary>
    UnreliableSequenced
}