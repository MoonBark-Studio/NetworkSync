using MoonBark.Framework.Logging;
using MoonBark.NetworkSync.Core.Interfaces;
using MoonBark.NetworkSync.Core.Messages;

namespace MoonBark.NetworkSync.Core.Services;

/// <summary>
/// Client-side prediction and reconciliation service.
/// Clients predict local changes and reconcile with authoritative server updates.
/// </summary>
public class ClientPredictionService : IClientPredictionService
{
    private readonly Dictionary<long, PredictedPlacementMessage> _predictions;
    private readonly Dictionary<long, PlacementResultMessage> _pendingCommands;
    private readonly ILocalOccupancyValidator _localValidator;
    private readonly object _lock = new();

    private readonly IFrameworkLogger _logger;

    public ClientPredictionService(ILocalOccupancyValidator localValidator)
        : this(localValidator, new ConsoleFrameworkLogger("ClientPrediction", FrameworkLogLevel.Debug)) { }

    public ClientPredictionService(ILocalOccupancyValidator localValidator, IFrameworkLogger logger)
    {
        _logger = logger;
        _predictions = new Dictionary<long, PredictedPlacementMessage>();
        _pendingCommands = new Dictionary<long, PlacementResultMessage>();
        _localValidator = localValidator;
    }

    public PlacementResultMessage PredictPlacement(PlacementCommandMessage command)
    {
        // Validate locally
        var isValid = _localValidator.IsPlacementValidLocally(
            command.X,
            command.Y,
            command.StructureType
        );

        var prediction = new PredictedPlacementMessage
        {
            CommandId = command.CommandId,
            X = command.X,
            Y = command.Y,
            StructureType = command.StructureType,
            Rotation = command.Rotation,
            IsValid = isValid,
            ValidationReason = isValid ? string.Empty : "Local validation failed"
        };

        var result = new PlacementResultMessage
        {
            CommandId = command.CommandId,
            Success = isValid,
            FailureReason = prediction.ValidationReason,
            ServerTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        lock (_lock)
        {
            _predictions[command.CommandId] = prediction;
            _pendingCommands[command.CommandId] = result;
        }

        _logger.Debug($"Predicted placement {command.CommandId}: {result.Success}");
        return result;
    }

    public void ReconcilePlacement(PlacementResultMessage serverResult, PlacementResultMessage predictedResult)
    {
        if (serverResult.Success == predictedResult.Success)
        {
            // Prediction was correct - no reconciliation needed
            _logger.Debug($"Prediction correct for command {serverResult.CommandId}");
            lock (_lock)
            {
                _predictions.Remove(serverResult.CommandId);
                _pendingCommands.Remove(serverResult.CommandId);
            }
            return;
        }

        // Prediction was wrong - reconcile
        _logger.Warning($"Prediction incorrect for command {serverResult.CommandId}");
        _logger.Debug($"  Predicted: {predictedResult.Success}");
        _logger.Debug($"  Server: {serverResult.Success}");

        lock (_lock)
        {
            // Remove the incorrect prediction
            _predictions.Remove(serverResult.CommandId);

            // Update pending command with server result
            _pendingCommands[serverResult.CommandId] = serverResult;
        }

        // Trigger reconciliation event if needed
        OnPredictionMismatch(serverResult, predictedResult);
    }

    public System.Collections.Generic.IEnumerable<PredictedPlacementMessage> GetPredictedPlacements()
    {
        lock (_lock)
        {
            return _predictions.Values.ToList();
        }
    }

    public void ClearPredictions()
    {
        lock (_lock)
        {
            _predictions.Clear();
            _pendingCommands.Clear();
        }
    }

    /// <summary>
    /// Gets a pending command result.
    /// </summary>
    public PlacementResultMessage? GetPendingCommandResult(long commandId)
    {
        lock (_lock)
        {
            return _pendingCommands.GetValueOrDefault(commandId);
        }
    }

    /// <summary>
    /// Marks a command as completed (server response received).
    /// </summary>
    public void MarkCommandCompleted(long commandId)
    {
        lock (_lock)
        {
            _pendingCommands.Remove(commandId);
        }
    }

    /// <summary>
    /// Event raised when a prediction mismatches the server result.
    /// </summary>
    public event EventHandler<PredictionMismatchEventArgs>? PredictionMismatch;

    protected virtual void OnPredictionMismatch(PlacementResultMessage serverResult, PlacementResultMessage predictedResult)
    {
        PredictionMismatch?.Invoke(this, new PredictionMismatchEventArgs
        {
            ServerResult = serverResult,
            PredictedResult = predictedResult
        });
    }
}

/// <summary>
/// Event arguments for prediction mismatch events.
/// </summary>
public class PredictionMismatchEventArgs : EventArgs
{
    public PlacementResultMessage ServerResult { get; set; } = null!;
    public PlacementResultMessage PredictedResult { get; set; } = null!;
}

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
