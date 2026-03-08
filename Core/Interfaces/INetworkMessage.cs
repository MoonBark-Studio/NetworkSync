namespace NetworkSync.Core.Interfaces;

/// <summary>
/// Base interface for all network messages.
/// All messages must be serializable and have a unique type identifier.
/// </summary>
public interface INetworkMessage
{
    /// <summary>
    /// Gets the message type identifier for serialization.
    /// </summary>
    byte MessageType { get; }

    /// <summary>
    /// Serializes the message to a byte array.
    /// </summary>
    byte[] Serialize();

    /// <summary>
    /// Deserializes the message from a byte array.
    /// </summary>
    void Deserialize(byte[] data);
}

/// <summary>
/// Base class for network messages with common serialization logic.
/// </summary>
public abstract class NetworkMessageBase : INetworkMessage
{
    /// <summary>
    /// Gets the message type identifier.
    /// Must be unique across all message types.
    /// </summary>
    public abstract byte MessageType { get; }

    /// <summary>
    /// Serializes the message to a byte array using JSON.
    /// </summary>
    public byte[] Serialize()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(this, GetType());
        return System.Text.Encoding.UTF8.GetBytes(json);
    }

    /// <summary>
    /// Deserializes the message from a byte array using JSON.
    /// </summary>
    public void Deserialize(byte[] data)
    {
        var json = System.Text.Encoding.UTF8.GetString(data);
        var deserialized = System.Text.Json.JsonSerializer.Deserialize(json, GetType());
        if (deserialized != null)
        {
            CopyFrom(deserialized);
        }
    }

    /// <summary>
    /// Copies properties from a deserialized object.
    /// Override this in derived classes to implement custom copy logic.
    /// </summary>
    protected virtual void CopyFrom(object source)
    {
        // Default implementation uses reflection
        var sourceType = source.GetType();
        var targetType = GetType();
        var properties = targetType.GetProperties();

        foreach (var property in properties)
        {
            if (property.CanWrite && property.CanRead)
            {
                var value = sourceType.GetProperty(property.Name)?.GetValue(source);
                property.SetValue(this, value);
            }
        }
    }
}
