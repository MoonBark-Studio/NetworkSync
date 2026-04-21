using MoonBark.NetworkSync.Core.Interfaces;
using MoonBark.NetworkSync.Core.Messages;
using MoonBark.NetworkSync.Core.Services;
using MoonBark.NetworkSync.Tests.StressTests.Mocks;
using Shouldly;
using Xunit;

namespace MoonBark.NetworkSync.Tests.StressTests;

public sealed class ReplicationServiceTests
{
    [Fact]
    public async Task PublishPlacementDeltaAsync_WithTrackedChanges_ShouldBroadcastDeltaAndClearPendingChanges()
    {
        MockNetworkTransport transport = new MockNetworkTransport();
        await transport.StartServerAsync(7777, 4);
        ReplicationService service = new ReplicationService(transport);

        service.TrackPlacementChange(11, 3, 4, PlacementDeltaChangeType.Added, "Wall", 1);
        service.TrackPlacementChange(12, 6, 8, PlacementDeltaChangeType.Modified, "Gate", 2);

        PlacementDeltaMessage delta = new PlacementDeltaMessage();
        await service.PublishPlacementDeltaAsync(delta);

        transport.BroadcastMessages.Count.ShouldBe(1);
        transport.BroadcastMessages[0].method.ShouldBe(DeliveryMethod.Unreliable);
        delta.TickNumber.ShouldBe(1);
        delta.Changes.Count.ShouldBe(2);
    }

    [Fact]
    public async Task PublishOccupancyDeltaAsync_WithTrackedChanges_ShouldBroadcastReliableDelta()
    {
        MockNetworkTransport transport = new MockNetworkTransport();
        await transport.StartServerAsync(7777, 4);
        ReplicationService service = new ReplicationService(transport);

        service.TrackOccupancyChange(2, 5, true, structureId: 77);

        OccupancyDeltaMessage delta = new OccupancyDeltaMessage();
        await service.PublishOccupancyDeltaAsync(delta);

        transport.BroadcastMessages.Count.ShouldBe(1);
        transport.BroadcastMessages[0].method.ShouldBe(DeliveryMethod.ReliableUnordered);
        delta.Changes.Count.ShouldBe(1);
        delta.Changes[0].StructureId.ShouldBe(77);
    }

    [Fact]
    public async Task ProcessPlacementDeltaAsync_ShouldRaiseEventAndTrackChangedCells()
    {
        MockNetworkTransport transport = new MockNetworkTransport();
        ReplicationService service = new ReplicationService(transport);
        PlacementDeltaMessage? received = null;

        service.PlacementDeltaReceived += (_, args) => received = args.Delta;

        PlacementDeltaMessage delta = new PlacementDeltaMessage();
        delta.Changes.Add(new PlacementDeltaMessage.PlacementChange { X = 9, Y = 10, Type = PlacementDeltaChangeType.Added, StructureType = "Wall" });

        await service.ProcessPlacementDeltaAsync(delta);

        received.ShouldNotBeNull();
        service.GetChangedCells().ShouldContain((9, 10));
    }

    [Fact]
    public async Task RequestWorldSnapshotAsync_WhenConnected_ShouldSendReliableRequestToServerPeer()
    {
        MockNetworkTransport transport = new MockNetworkTransport();
        await transport.ConnectAsync("127.0.0.1", 7777);
        ReplicationService service = new ReplicationService(transport);

        await service.RequestWorldSnapshotAsync();

        transport.SentMessages.Count.ShouldBe(1);
        transport.SentMessages[0].peerId.ShouldBe(0);
        transport.SentMessages[0].method.ShouldBe(DeliveryMethod.ReliableOrdered);
        transport.SentMessages[0].message.ShouldBeOfType<WorldSnapshotMessage>();
    }

    [Fact]
    public void ClearChangedCells_ShouldRemoveTrackedCells()
    {
        MockNetworkTransport transport = new MockNetworkTransport();
        ReplicationService service = new ReplicationService(transport);

        service.TrackOccupancyChange(1, 1, true);
        service.GetChangedCells().Count().ShouldBe(1);

        service.ClearChangedCells();

        service.GetChangedCells().ShouldBeEmpty();
    }
}
