namespace MoonBark.NetworkSync.Core.Interfaces;

/// <summary>
/// Defines the contract for server-authoritative command validation.
/// The server is the source of truth for all game state changes.
/// </summary>
public interface IServerAuthorityService
{
    /// <summary>
    /// Validates a placement command against authoritative core state.
    /// </summary>
    /// <param name="command">The placement command to validate.</param>
    /// <returns>True if the command is valid, false otherwise.</returns>
    Task<bool> ValidatePlacementCommandAsync(Messages.PlacementCommandMessage command);

    /// <summary>
    /// Applies a validated placement command to authoritative state.
    /// </summary>
    /// <param name="command">The placement command to apply.</param>
    /// <returns>The result of applying the command.</returns>
    Task<Messages.PlacementResultMessage> ApplyPlacementCommandAsync(Messages.PlacementCommandMessage command);

    /// <summary>
    /// Gets the current authoritative occupancy state for a cell.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <returns>The occupancy information for the cell.</returns>
    Task<Messages.CellOccupancyMessage> GetCellOccupancyAsync(int x, int y);

    /// <summary>
    /// Gets the current authoritative occupancy state for a region.
    /// </summary>
    /// <param name="x">The starting X coordinate.</param>
    /// <param name="y">The starting Y coordinate.</param>
    /// <param name="width">The width of the region.</param>
    /// <param name="height">The height of the region.</param>
    /// <returns>The occupancy information for the region.</returns>
    Task<Messages.RegionOccupancyMessage> GetRegionOccupancyAsync(int x, int y, int width, int height);
}

/// <summary>
/// Defines the contract for client-side prediction and reconciliation.
/// Clients predict local changes and reconcile with authoritative server updates.
/// </summary>
public interface IClientPredictionService
{
    /// <summary>
    /// Predicts the result of a local placement command.
    /// </summary>
    /// <param name="command">The placement command to predict.</param>
    /// <returns>The predicted placement result.</returns>
    Messages.PlacementResultMessage PredictPlacement(Messages.PlacementCommandMessage command);

    /// <summary>
    /// Reconciles local predicted state with authoritative server state.
    /// </summary>
    /// <param name="serverResult">The authoritative server result.</param>
    /// <param name="predictedResult">The local predicted result.</param>
    void ReconcilePlacement(Messages.PlacementResultMessage serverResult, Messages.PlacementResultMessage predictedResult);

    /// <summary>
    /// Gets the current predicted placement overlay state.
    /// </summary>
    /// <returns>A collection of predicted placement cells.</returns>
    System.Collections.Generic.IEnumerable<Messages.PredictedPlacementMessage> GetPredictedPlacements();

    /// <summary>
    /// Clears all predicted placements.
    /// </summary>
    void ClearPredictions();
}
