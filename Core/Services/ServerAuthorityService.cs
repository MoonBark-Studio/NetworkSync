using MoonBark.Framework.Logging;
using MoonBark.NetworkSync.Core.Interfaces;
using MoonBark.NetworkSync.Core.Messages;

namespace MoonBark.NetworkSync.Core.Services;

/// <summary>
/// Server-authoritative command validation and state management.
/// The server is the source of truth for all game state changes.
/// </summary>
public class ServerAuthorityService : IServerAuthorityService
{
    private readonly ICoreOccupancyProvider _occupancyProvider;
    private readonly Dictionary<long, PlacementResultMessage> _commandResults;
    private readonly object _lock = new();

    private readonly IFrameworkLogger _logger;

    public ServerAuthorityService(ICoreOccupancyProvider occupancyProvider)
        : this(occupancyProvider, new ConsoleFrameworkLogger("ServerAuthority", FrameworkLogLevel.Debug)) { }

    public ServerAuthorityService(ICoreOccupancyProvider occupancyProvider, IFrameworkLogger logger)
    {
        _logger = logger;
        _occupancyProvider = occupancyProvider;
        _commandResults = new Dictionary<long, PlacementResultMessage>();
    }

    public async Task<bool> ValidatePlacementCommandAsync(PlacementCommandMessage command)
    {
        // Validate against core occupancy state
        var isValid = await _occupancyProvider.IsPlacementValidAsync(
            command.X,
            command.Y,
            command.StructureType
        );

        _logger.Debug($"Validating placement at ({command.X}, {command.Y}): {isValid}");
        return isValid;
    }

    public async Task<PlacementResultMessage> ApplyPlacementCommandAsync(PlacementCommandMessage command)
    {
        // Validate first
        var isValid = await ValidatePlacementCommandAsync(command);

        var result = new PlacementResultMessage
        {
            CommandId = command.CommandId,
            Success = isValid,
            FailureReason = isValid ? string.Empty : "Placement validation failed",
            ServerTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        // If valid, apply to core state
        if (isValid)
        {
            await _occupancyProvider.ApplyPlacementAsync(
                command.X,
                command.Y,
                command.StructureType,
                command.Rotation
            );
        }

        // Cache the result
        lock (_lock)
        {
            _commandResults[command.CommandId] = result;
        }

        _logger.Debug($"Applied placement command {command.CommandId}: {result.Success}");
        return result;
    }

    public async Task<CellOccupancyMessage> GetCellOccupancyAsync(int x, int y)
    {
        var occupancy = await _occupancyProvider.GetCellOccupancyAsync(x, y);

        return new CellOccupancyMessage
        {
            X = x,
            Y = y,
            Occupied = occupancy.Occupied,
            EntityId = occupancy.EntityId,
            StructureId = occupancy.StructureId
        };
    }

    public async Task<RegionOccupancyMessage> GetRegionOccupancyAsync(int x, int y, int width, int height)
    {
        var regionOccupancy = await _occupancyProvider.GetRegionOccupancyAsync(x, y, width, height);

        return new RegionOccupancyMessage
        {
            X = x,
            Y = y,
            Width = width,
            Height = height,
            Cells = regionOccupancy.Cells.Select(c => new Messages.CellOccupancyData
            {
                Occupied = c.Occupied,
                EntityId = c.EntityId,
                StructureId = c.StructureId
            }).ToList()
        };
    }

    /// <summary>
    /// Gets the result of a previously processed command.
    /// </summary>
    public PlacementResultMessage? GetCommandResult(long commandId)
    {
        lock (_lock)
        {
            return _commandResults.GetValueOrDefault(commandId);
        }
    }

    /// <summary>
    /// Clears old command results to prevent memory leaks.
    /// </summary>
    public void ClearOldResults(long beforeTick)
    {
        lock (_lock)
        {
            var oldResults = _commandResults
                .Where(kvp => kvp.Value.ServerTimestamp < beforeTick)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in oldResults)
            {
                _commandResults.Remove(key);
            }
        }
    }
}

/// <summary>
/// Interface for core occupancy provider.
/// This is the bridge between the networking layer and the game's core occupancy system.
/// </summary>
public interface ICoreOccupancyProvider
{
    /// <summary>
    /// Checks if a placement is valid at the specified coordinates.
    /// </summary>
    Task<bool> IsPlacementValidAsync(int x, int y, string structureType);

    /// <summary>
    /// Applies a placement to the core occupancy state.
    /// </summary>
    Task ApplyPlacementAsync(int x, int y, string structureType, int rotation);

    /// <summary>
    /// Gets the occupancy state for a single cell.
    /// </summary>
    Task<CellOccupancyData> GetCellOccupancyAsync(int x, int y);

    /// <summary>
    /// Gets the occupancy state for a region.
    /// </summary>
    Task<RegionOccupancyData> GetRegionOccupancyAsync(int x, int y, int width, int height);
}

/// <summary>
/// Data structure for cell occupancy information.
/// </summary>
public class CellOccupancyData
{
    public bool Occupied { get; set; }
    public long? EntityId { get; set; }
    public long? StructureId { get; set; }
}

/// <summary>
/// Data structure for region occupancy information.
/// </summary>
public class RegionOccupancyData
{
    public List<CellOccupancyData> Cells { get; set; } = new();
}
