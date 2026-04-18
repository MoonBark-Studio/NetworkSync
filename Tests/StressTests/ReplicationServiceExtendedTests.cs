using MoonBark.NetworkSync.Core.Messages;
using MoonBark.NetworkSync.Core.Services;
using MoonBark.NetworkSync.Tests.StressTests.Mocks;
using Shouldly;
using Xunit;

namespace MoonBark.NetworkSync.Tests.StressTests;

public sealed class ReplicationServiceExtendedTests
{
    [Fact]
    public async Task PublishPlacementDeltaAsync_WhenNotConnected_ShouldNotBroadcast()
    {
        var transport = new MockNetworkTransport();
        var service = new ReplicationService(transport);

        service.TrackPlacementChange(1, 0, 0, PlacementDeltaMessage.ChangeType.Added);

        await service.PublishPlacementDeltaAsync(new PlacementDeltaMessage());

        transport.BroadcastMessages.ShouldBeEmpty();
    }

    [Fact]
    public async Task PublishOccupancyDeltaAsync_WhenNotConnected_ShouldNotBroadcast()
    {
        var transport = new MockNetworkTransport();
        var service = new ReplicationService(transport);

        service.TrackOccupancyChange(0, 0, true);

        await service.PublishOccupancyDeltaAsync(new OccupancyDeltaMessage());

        transport.BroadcastMessages.ShouldBeEmpty();
    }

    [Fact]
    public async Task RequestWorldSnapshotAsync_WhenNotConnected_ShouldNotSend()
    {
        var transport = new MockNetworkTransport();
        var service = new ReplicationService(transport);

        await service.RequestWorldSnapshotAsync();

        transport.SentMessages.ShouldBeEmpty();
    }

    [Fact]
    public async Task ProcessOccupancyDeltaAsync_ShouldRaiseEventAndTrackChangedCells()
    {
        var transport = new MockNetworkTransport();
        var service = new ReplicationService(transport);
        OccupancyDeltaMessage? received = null;
        service.OccupancyDeltaReceived += (_, args) => received = args.Delta;

        var delta = new OccupancyDeltaMessage();
        delta.Changes.Add(new OccupancyDeltaMessage.OccupancyChange
        {
            X = 3, Y = 7, Occupied = true, StructureId = 42
        });

        await service.ProcessOccupancyDeltaAsync(delta);

        received.ShouldNotBeNull();
        received.Changes.Count.ShouldBe(1);
        received.Changes[0].StructureId.ShouldBe(42);
        service.GetChangedCells().ShouldContain((3, 7));
    }

    [Fact]
    public void Dispose_ShouldUnsubscribeFromTransportEvents()
    {
        var transport = new MockNetworkTransport();
        var service = new ReplicationService(transport);

        service.Dispose();

        transport.RaiseMessageReceived(0, new PlacementDeltaMessage());
    }

    [Fact]
    public async Task OnMessageReceived_WhenPlacementDelta_ShouldProcessAndRaiseEvent()
    {
        var transport = new MockNetworkTransport();
        var service = new ReplicationService(transport);
        PlacementDeltaMessage? received = null;
        service.PlacementDeltaReceived += (_, args) => received = args.Delta;

        var delta = new PlacementDeltaMessage();
        delta.Changes.Add(new PlacementDeltaMessage.PlacementChange
        {
            X = 1, Y = 2, Type = PlacementDeltaMessage.ChangeType.Added
        });

        transport.RaiseMessageReceived(0, delta);

        await Task.Delay(100);

        received.ShouldNotBeNull();
        received.Changes[0].X.ShouldBe(1);
    }

    [Fact]
    public async Task OnMessageReceived_WhenOccupancyDelta_ShouldProcessAndRaiseEvent()
    {
        var transport = new MockNetworkTransport();
        var service = new ReplicationService(transport);
        OccupancyDeltaMessage? received = null;
        service.OccupancyDeltaReceived += (_, args) => received = args.Delta;

        var delta = new OccupancyDeltaMessage();
        delta.Changes.Add(new OccupancyDeltaMessage.OccupancyChange
        {
            X = 5, Y = 6, Occupied = true
        });

        transport.RaiseMessageReceived(0, delta);

        await Task.Delay(100);

        received.ShouldNotBeNull();
    }

    [Fact]
    public async Task OnMessageReceived_WhenWorldSnapshot_ShouldRaiseEvent()
    {
        var transport = new MockNetworkTransport();
        var service = new ReplicationService(transport);
        WorldSnapshotMessage? received = null;
        service.WorldSnapshotReceived += (_, args) => received = args.Snapshot;

        var snapshot = new WorldSnapshotMessage { TickNumber = 42 };

        transport.RaiseMessageReceived(0, snapshot);

        await Task.Delay(100);

        received.ShouldNotBeNull();
        received.TickNumber.ShouldBe(42);
    }

    [Fact]
    public async Task PublishPlacementDeltaAsync_ShouldIncrementTickAndClearPendingChanges()
    {
        var transport = new MockNetworkTransport();
        await transport.StartServerAsync(7777, 4);
        var service = new ReplicationService(transport);

        service.TrackPlacementChange(1, 1, 1, PlacementDeltaMessage.ChangeType.Added, "A");
        service.TrackPlacementChange(2, 2, 2, PlacementDeltaMessage.ChangeType.Added, "B");

        var delta1 = new PlacementDeltaMessage();
        await service.PublishPlacementDeltaAsync(delta1);
        delta1.TickNumber.ShouldBe(1);
        delta1.Changes.Count.ShouldBe(2);

        var delta2 = new PlacementDeltaMessage();
        await service.PublishPlacementDeltaAsync(delta2);
        delta2.TickNumber.ShouldBe(2);
        delta2.Changes.Count.ShouldBe(0);
    }
}
