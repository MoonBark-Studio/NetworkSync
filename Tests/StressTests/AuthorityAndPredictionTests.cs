using MoonBark.NetworkSync.Core.Messages;
using MoonBark.NetworkSync.Core.Services;
using MoonBark.NetworkSync.Tests.StressTests.Mocks;
using Shouldly;
using Xunit;

namespace MoonBark.NetworkSync.Tests.StressTests;

public sealed class AuthorityAndPredictionTests
{
    [Fact]
    public async Task ApplyPlacementCommandAsync_WithValidPlacement_ShouldPersistAndCacheResult()
    {
        TestOccupancyProvider occupancyProvider = new TestOccupancyProvider(32);
        ServerAuthorityService authority = new ServerAuthorityService(occupancyProvider);
        PlacementCommandMessage command = new PlacementCommandMessage
        {
            CommandId = 101,
            ClientId = 1,
            X = 4,
            Y = 7,
            StructureType = "Wall",
            Rotation = 0,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        PlacementResultMessage result = await authority.ApplyPlacementCommandAsync(command);

        result.Success.ShouldBeTrue();
        authority.GetCommandResult(101).ShouldNotBeNull();
        occupancyProvider.GetPlacementCount().ShouldBe(1);
    }

    [Fact]
    public async Task ApplyPlacementCommandAsync_WithInvalidPlacement_ShouldReturnFailureWithoutPersisting()
    {
        TestOccupancyProvider occupancyProvider = new TestOccupancyProvider(8);
        await occupancyProvider.ApplyPlacementAsync(2, 2, "Existing", 0);
        ServerAuthorityService authority = new ServerAuthorityService(occupancyProvider);
        PlacementCommandMessage command = new PlacementCommandMessage
        {
            CommandId = 202,
            ClientId = 1,
            X = 2,
            Y = 2,
            StructureType = "Wall",
            Rotation = 0,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        PlacementResultMessage result = await authority.ApplyPlacementCommandAsync(command);

        result.Success.ShouldBeFalse();
        occupancyProvider.GetPlacementCount().ShouldBe(1);
        result.FailureReason.ShouldNotBeEmpty();
    }

    [Fact]
    public void PredictPlacement_WithValidLocalPlacement_ShouldTrackPredictionAndPendingResult()
    {
        TestLocalValidator localValidator = new TestLocalValidator(32);
        ClientPredictionService prediction = new ClientPredictionService(localValidator);
        PlacementCommandMessage command = new PlacementCommandMessage
        {
            CommandId = 301,
            ClientId = 1,
            X = 5,
            Y = 6,
            StructureType = "Gate",
            Rotation = 1,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        PlacementResultMessage result = prediction.PredictPlacement(command);

        result.Success.ShouldBeTrue();
        prediction.GetPredictedPlacements().Count().ShouldBe(1);
        prediction.GetPendingCommandResult(301).ShouldNotBeNull();
    }

    [Fact]
    public void ReconcilePlacement_WithMismatch_ShouldRaiseEventAndRetainServerResult()
    {
        TestLocalValidator localValidator = new TestLocalValidator(32);
        ClientPredictionService prediction = new ClientPredictionService(localValidator);
        PlacementCommandMessage command = new PlacementCommandMessage
        {
            CommandId = 401,
            ClientId = 2,
            X = 1,
            Y = 1,
            StructureType = "Tower",
            Rotation = 0,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        PlacementResultMessage predicted = prediction.PredictPlacement(command);
        PlacementResultMessage serverResult = new PlacementResultMessage
        {
            CommandId = 401,
            Success = false,
            FailureReason = "Rejected by server",
            ServerTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        PredictionMismatchEventArgs? mismatch = null;
        prediction.PredictionMismatch += (_, args) => mismatch = args;

        prediction.ReconcilePlacement(serverResult, predicted);

        mismatch.ShouldNotBeNull();
        prediction.GetPendingCommandResult(401).ShouldNotBeNull();
        prediction.GetPendingCommandResult(401)!.Success.ShouldBeFalse();
        prediction.GetPredictedPlacements().ShouldBeEmpty();
    }
}
