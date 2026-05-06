using MoonBark.NetworkSync.Core.Messages;
using MoonBark.NetworkSync.Core.Services;
using MoonBark.NetworkSync.Tests.StressTests.Mocks;
using Shouldly;
using Xunit;

namespace MoonBark.NetworkSync.Tests.StressTests;

public sealed class ClientPredictionExtendedTests
{
    [Fact]
    public void ReconcilePlacement_WithCorrectPrediction_ShouldRemoveFromPredictionsAndPending()
    {
        var validator = new TestLocalValidator(32);
        var prediction = new ClientPredictionService(validator);
        var command = new PlacementCommandMessage
        {
            CommandId = 100, ClientId = 1, X = 5, Y = 5,
            StructureType = "Wall", Timestamp = 1000
        };

        var predicted = prediction.PredictPlacement(command);
        var serverResult = new PlacementResultMessage
        {
            CommandId = 100,
            Success = true,
            ServerTimestamp = 2000
        };

        PredictionMismatchEventArgs? mismatch = null;
        prediction.PredictionMismatch += (_, args) => mismatch = args;

        prediction.ReconcilePlacement(serverResult, predicted);

        mismatch.ShouldBeNull();
        prediction.GetPredictedPlacements().ShouldBeEmpty();
        prediction.GetPendingCommandResult(100).ShouldBeNull();
    }

    [Fact]
    public void ClearPredictions_ShouldRemoveAllPredictionsAndPendingCommands()
    {
        var validator = new TestLocalValidator(32);
        var prediction = new ClientPredictionService(validator);

        prediction.PredictPlacement(new PlacementCommandMessage
        {
            CommandId = 1, X = 1, Y = 1, StructureType = "Wall"
        });
        prediction.PredictPlacement(new PlacementCommandMessage
        {
            CommandId = 2, X = 2, Y = 2, StructureType = "Gate"
        });

        prediction.GetPredictedPlacements().Count().ShouldBe(2);
        prediction.GetPendingCommandResult(1).ShouldNotBeNull();

        prediction.ClearPredictions();

        prediction.GetPredictedPlacements().ShouldBeEmpty();
        prediction.GetPendingCommandResult(1).ShouldBeNull();
        prediction.GetPendingCommandResult(2).ShouldBeNull();
    }

    [Fact]
    public void MarkCommandCompleted_ShouldRemoveFromPendingCommands()
    {
        var validator = new TestLocalValidator(32);
        var prediction = new ClientPredictionService(validator);

        prediction.PredictPlacement(new PlacementCommandMessage
        {
            CommandId = 50, X = 3, Y = 3, StructureType = "Tower"
        });

        prediction.GetPendingCommandResult(50).ShouldNotBeNull();

        prediction.MarkCommandCompleted(50);

        prediction.GetPendingCommandResult(50).ShouldBeNull();
    }

    [Fact]
    public void PredictPlacement_WithInvalidLocalPlacement_ShouldTrackFailedPrediction()
    {
        var validator = new TestLocalValidator(8);
        validator.UpdateLocalOccupancy(4, 4, true);
        var prediction = new ClientPredictionService(validator);

        var result = prediction.PredictPlacement(new PlacementCommandMessage
        {
            CommandId = 99, X = 4, Y = 4, StructureType = "Wall"
        });

        result.Success.ShouldBeFalse();
        result.FailureReason.ShouldNotBeEmpty();
        prediction.GetPredictedPlacements().Count().ShouldBe(1);
    }
}
