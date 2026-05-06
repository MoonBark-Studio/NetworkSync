using MoonBark.NetworkSync.Tests.StressTests.Infrastructure;
using MoonBark.NetworkSync.Tests.StressTests.Mocks;
using Shouldly;
using Xunit;

namespace MoonBark.NetworkSync.Tests.StressTests;

public sealed class StressInfrastructureTests
{
    [Fact]
    public async Task TestOccupancyProvider_ShouldRejectOutOfBoundsAndDuplicates()
    {
        TestOccupancyProvider provider = new TestOccupancyProvider(4);

        (await provider.IsPlacementValidAsync(1, 1, "Wall")).ShouldBeTrue();
        await provider.ApplyPlacementAsync(1, 1, "Wall", 0);
        (await provider.IsPlacementValidAsync(1, 1, "Wall")).ShouldBeFalse();
        (await provider.IsPlacementValidAsync(8, 8, "Wall")).ShouldBeFalse();
    }

    [Fact]
    public void TestLocalValidator_ShouldTrackOccupancyTransitions()
    {
        TestLocalValidator validator = new TestLocalValidator(10);

        validator.IsPlacementValidLocally(2, 3, "Wall").ShouldBeTrue();
        validator.UpdateLocalOccupancy(2, 3, true, structureId: 99);
        validator.IsPlacementValidLocally(2, 3, "Wall").ShouldBeFalse();
        validator.GetLocalPlacementCount().ShouldBe(1);
        validator.UpdateLocalOccupancy(2, 3, false);
        validator.GetLocalPlacementCount().ShouldBe(0);
    }

    [Fact]
    public void TestMetrics_ShouldAggregateConnectionMessageAndPlacementSnapshots()
    {
        TestMetrics metrics = new TestMetrics();

        metrics.RecordConnection(true, 25);
        metrics.RecordConnection(false, 0);
        metrics.RecordMessageSent();
        metrics.RecordMessageReceived(12);
        metrics.RecordPlacementCommand(true);
        metrics.RecordPlacementCommand(false);
        metrics.RecordServerPlacement(7);
        metrics.RecordClientPlacement(6);
        metrics.RecordSyncMismatch();
        metrics.Stop();

        TestMetricsSnapshot snapshot = metrics.GetSnapshot();

        snapshot.TotalConnections.ShouldBe(2);
        snapshot.SuccessfulConnections.ShouldBe(1);
        snapshot.FailedConnections.ShouldBe(1);
        snapshot.TotalMessagesSent.ShouldBe(1);
        snapshot.TotalMessagesReceived.ShouldBe(1);
        snapshot.SuccessfulPlacements.ShouldBe(1);
        snapshot.FailedPlacements.ShouldBe(1);
        snapshot.ServerPlacements.ShouldBe(7);
        snapshot.ClientPlacements.ShouldBe(6);
        snapshot.SyncMismatches.ShouldBe(1);
    }
}
