using MoonBark.NetworkSync.Core.Messages;
using MoonBark.NetworkSync.Core.Services;
using MoonBark.NetworkSync.Tests.StressTests.Mocks;
using Shouldly;
using Xunit;

namespace MoonBark.NetworkSync.Tests.StressTests;

public sealed class ServerAuthorityExtendedTests
{
    [Fact]
    public async Task GetCellOccupancyAsync_WhenCellIsOccupied_ShouldReturnOccupancyData()
    {
        var provider = new TestOccupancyProvider(32);
        await provider.ApplyPlacementAsync(5, 10, "Wall", 0);
        var authority = new ServerAuthorityService(provider);

        var result = await authority.GetCellOccupancyAsync(5, 10);

        result.X.ShouldBe(5);
        result.Y.ShouldBe(10);
        result.Occupied.ShouldBeTrue();
        result.StructureId.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetCellOccupancyAsync_WhenCellIsEmpty_ShouldReturnUnoccupied()
    {
        var provider = new TestOccupancyProvider(32);
        var authority = new ServerAuthorityService(provider);

        var result = await authority.GetCellOccupancyAsync(5, 10);

        result.Occupied.ShouldBeFalse();
        result.EntityId.ShouldBeNull();
        result.StructureId.ShouldBeNull();
    }

    [Fact]
    public async Task GetRegionOccupancyAsync_ShouldReturnAllCellsInRegion()
    {
        var provider = new TestOccupancyProvider(32);
        await provider.ApplyPlacementAsync(0, 0, "Wall", 0);
        await provider.ApplyPlacementAsync(1, 1, "Gate", 0);
        var authority = new ServerAuthorityService(provider);

        var result = await authority.GetRegionOccupancyAsync(0, 0, 2, 2);

        result.X.ShouldBe(0);
        result.Y.ShouldBe(0);
        result.Width.ShouldBe(2);
        result.Height.ShouldBe(2);
        result.Cells.Count.ShouldBe(4);
        result.Cells[0].Occupied.ShouldBeTrue();
        result.Cells[3].Occupied.ShouldBeTrue();
    }

    [Fact]
    public async Task ClearOldResults_ShouldRemoveResultsOlderThanGivenTimestamp()
    {
        var provider = new TestOccupancyProvider(32);
        var authority = new ServerAuthorityService(provider);

        var command1 = new PlacementCommandMessage
        {
            CommandId = 1, ClientId = 1, X = 1, Y = 1,
            StructureType = "Wall", Timestamp = 1000
        };
        var command2 = new PlacementCommandMessage
        {
            CommandId = 2, ClientId = 1, X = 2, Y = 2,
            StructureType = "Wall", Timestamp = 1000
        };

        var result1 = await authority.ApplyPlacementCommandAsync(command1);
        var result2 = await authority.ApplyPlacementCommandAsync(command2);

        authority.ClearOldResults(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 1);

        authority.GetCommandResult(1).ShouldBeNull();
        authority.GetCommandResult(2).ShouldBeNull();
    }

    [Fact]
    public async Task ClearOldResults_ShouldNotRemoveRecentResults()
    {
        var provider = new TestOccupancyProvider(32);
        var authority = new ServerAuthorityService(provider);

        var command = new PlacementCommandMessage
        {
            CommandId = 1, ClientId = 1, X = 1, Y = 1,
            StructureType = "Wall", Timestamp = 1000
        };

        await authority.ApplyPlacementCommandAsync(command);

        authority.ClearOldResults(0);

        authority.GetCommandResult(1).ShouldNotBeNull();
    }
}
