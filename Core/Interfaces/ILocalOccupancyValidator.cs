namespace MoonBark.NetworkSync.Core.Interfaces;

/// <summary>
/// Interface for local occupancy validation.
/// Used by clients to predict placement validity before server response.
/// </summary>
public interface ILocalOccupancyValidator
{
    /// <summary>
    /// Validates placement locally using client-side occupancy state.
    /// </summary>
    bool IsPlacementValidLocally(int x, int y, string structureType);

    /// <summary>
    /// Updates local occupancy state based on server updates.
    /// </summary>
    void UpdateLocalOccupancy(int x, int y, bool occupied, long? entityId = null, long? structureId = null);
}
