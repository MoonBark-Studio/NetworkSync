using MoonBark.NetworkSync.Core.Interfaces;
using MoonBark.NetworkSync.Core.Messages;
using Shouldly;
using Xunit;

namespace MoonBark.NetworkSync.Tests.StressTests;

public sealed class NetworkMessageSerializationTests
{
    [Fact]
    public void PlacementCommandMessage_SerializeDeserialize_RoundTrip_ShouldPreserveAllProperties()
    {
        var original = new PlacementCommandMessage
        {
            CommandId = 42,
            ClientId = 7,
            X = 10,
            Y = 20,
            StructureType = "Wall",
            Rotation = 3,
            Timestamp = 1700000000
        };

        byte[] data = original.Serialize();
        var deserialized = new PlacementCommandMessage();
        deserialized.Deserialize(data);

        deserialized.CommandId.ShouldBe(original.CommandId);
        deserialized.ClientId.ShouldBe(original.ClientId);
        deserialized.X.ShouldBe(original.X);
        deserialized.Y.ShouldBe(original.Y);
        deserialized.StructureType.ShouldBe(original.StructureType);
        deserialized.Rotation.ShouldBe(original.Rotation);
        deserialized.Timestamp.ShouldBe(original.Timestamp);
    }

    [Fact]
    public void PlacementResultMessage_SerializeDeserialize_RoundTrip_ShouldPreserveAllProperties()
    {
        var original = new PlacementResultMessage
        {
            CommandId = 99,
            Success = false,
            FailureReason = "Cell occupied",
            ServerTimestamp = 1700000001
        };

        byte[] data = original.Serialize();
        var deserialized = new PlacementResultMessage();
        deserialized.Deserialize(data);

        deserialized.CommandId.ShouldBe(original.CommandId);
        deserialized.Success.ShouldBe(original.Success);
        deserialized.FailureReason.ShouldBe(original.FailureReason);
        deserialized.ServerTimestamp.ShouldBe(original.ServerTimestamp);
    }

    [Fact]
    public void ConnectRequestMessage_SerializeDeserialize_RoundTrip_ShouldPreserveAllProperties()
    {
        var original = new ConnectRequestMessage
        {
            GameVersion = "2.1.0",
            ClientId = "client-abc-123"
        };

        byte[] data = original.Serialize();
        var deserialized = new ConnectRequestMessage();
        deserialized.Deserialize(data);

        deserialized.GameVersion.ShouldBe(original.GameVersion);
        deserialized.ClientId.ShouldBe(original.ClientId);
    }

    [Fact]
    public void ConnectResponseMessage_SerializeDeserialize_RoundTrip_ShouldPreserveAllProperties()
    {
        var original = new ConnectResponseMessage
        {
            Accepted = true,
            RejectReason = "",
            AssignedPeerId = 5,
            ServerGameVersion = "2.1.0"
        };

        byte[] data = original.Serialize();
        var deserialized = new ConnectResponseMessage();
        deserialized.Deserialize(data);

        deserialized.Accepted.ShouldBe(original.Accepted);
        deserialized.AssignedPeerId.ShouldBe(original.AssignedPeerId);
        deserialized.ServerGameVersion.ShouldBe(original.ServerGameVersion);
    }

    [Fact]
    public void DisconnectMessage_SerializeDeserialize_RoundTrip_ShouldPreserveReason()
    {
        var original = new DisconnectMessage { Reason = "Client exit" };

        byte[] data = original.Serialize();
        var deserialized = new DisconnectMessage();
        deserialized.Deserialize(data);

        deserialized.Reason.ShouldBe(original.Reason);
    }

    [Fact]
    public void PlacementDeltaMessage_SerializeDeserialize_RoundTrip_ShouldPreserveChanges()
    {
        var original = new PlacementDeltaMessage
        {
            TickNumber = 100,
            Changes = new List<PlacementDeltaMessage.PlacementChange>
            {
                new() { X = 1, Y = 2, Type = PlacementDeltaChangeType.Added, StructureType = "Wall", Rotation = 0 },
                new() { X = 3, Y = 4, Type = PlacementDeltaChangeType.Removed, StructureType = "Gate", Rotation = 1 }
            }
        };

        byte[] data = original.Serialize();
        var deserialized = new PlacementDeltaMessage();
        deserialized.Deserialize(data);

        deserialized.TickNumber.ShouldBe(original.TickNumber);
        deserialized.Changes.Count.ShouldBe(2);
        deserialized.Changes[0].X.ShouldBe(1);
        deserialized.Changes[0].Type.ShouldBe(PlacementDeltaChangeType.Added);
        deserialized.Changes[1].Type.ShouldBe(PlacementDeltaChangeType.Removed);
    }

    [Fact]
    public void OccupancyDeltaMessage_SerializeDeserialize_RoundTrip_ShouldPreserveChanges()
    {
        var original = new OccupancyDeltaMessage
        {
            TickNumber = 50,
            Changes = new List<OccupancyDeltaMessage.OccupancyChange>
            {
                new() { X = 5, Y = 6, Occupied = true, EntityId = 100, StructureId = null },
                new() { X = 7, Y = 8, Occupied = false, EntityId = null, StructureId = null }
            }
        };

        byte[] data = original.Serialize();
        var deserialized = new OccupancyDeltaMessage();
        deserialized.Deserialize(data);

        deserialized.TickNumber.ShouldBe(original.TickNumber);
        deserialized.Changes.Count.ShouldBe(2);
        deserialized.Changes[0].Occupied.ShouldBeTrue();
        deserialized.Changes[0].EntityId.ShouldBe(100);
        deserialized.Changes[1].Occupied.ShouldBeFalse();
    }

    [Fact]
    public void CellOccupancyMessage_SerializeDeserialize_RoundTrip_ShouldPreserveAllProperties()
    {
        var original = new CellOccupancyMessage
        {
            X = 10,
            Y = 20,
            Occupied = true,
            EntityId = 55,
            StructureId = 66
        };

        byte[] data = original.Serialize();
        var deserialized = new CellOccupancyMessage();
        deserialized.Deserialize(data);

        deserialized.X.ShouldBe(original.X);
        deserialized.Y.ShouldBe(original.Y);
        deserialized.Occupied.ShouldBeTrue();
        deserialized.EntityId.ShouldBe(55);
        deserialized.StructureId.ShouldBe(66);
    }

    [Fact]
    public void RegionOccupancyMessage_SerializeDeserialize_RoundTrip_ShouldPreserveCells()
    {
        var original = new RegionOccupancyMessage
        {
            X = 0,
            Y = 0,
            Width = 2,
            Height = 2,
            Cells = new List<CellOccupancyData>
            {
                new() { Occupied = true, EntityId = 1, StructureId = null },
                new() { Occupied = false, EntityId = null, StructureId = null },
                new() { Occupied = true, EntityId = null, StructureId = 2 },
                new() { Occupied = false, EntityId = null, StructureId = null }
            }
        };

        byte[] data = original.Serialize();
        var deserialized = new RegionOccupancyMessage();
        deserialized.Deserialize(data);

        deserialized.X.ShouldBe(0);
        deserialized.Y.ShouldBe(0);
        deserialized.Width.ShouldBe(2);
        deserialized.Height.ShouldBe(2);
        deserialized.Cells.Count.ShouldBe(4);
        deserialized.Cells[0].Occupied.ShouldBeTrue();
        deserialized.Cells[2].StructureId.ShouldBe(2);
    }

    [Fact]
    public void WorldSnapshotMessage_SerializeDeserialize_RoundTrip_ShouldPreservePlacementsAndOccupancy()
    {
        var original = new WorldSnapshotMessage
        {
            TickNumber = 999,
            ServerTimestamp = 1700000042,
            Placements = new List<WorldSnapshotMessage.PlacementSnapshotEntry>
            {
                new() { X = 1, Y = 2, StructureType = "Tower", Rotation = 0, StructureId = 10 }
            },
            Occupancy = new List<WorldSnapshotMessage.OccupancySnapshotEntry>
            {
                new() { X = 1, Y = 2, Occupied = true, EntityId = 5, StructureId = 10 }
            }
        };

        byte[] data = original.Serialize();
        var deserialized = new WorldSnapshotMessage();
        deserialized.Deserialize(data);

        deserialized.TickNumber.ShouldBe(999);
        deserialized.Placements.Count.ShouldBe(1);
        deserialized.Placements[0].StructureType.ShouldBe("Tower");
        deserialized.Occupancy[0].Occupied.ShouldBeTrue();
    }

    [Fact]
    public void ChunkDeltaMessage_SerializeDeserialize_RoundTrip_ShouldPreserveTerrain()
    {
        var original = new ChunkDeltaMessage
        {
            ChunkX = 3,
            ChunkY = 4,
            ChunkSize = 16,
            Terrain = new List<TerrainCellData>
            {
                new() { LocalX = 0, LocalY = 0, TerrainType = "Grass", Blocked = false },
                new() { LocalX = 1, LocalY = 0, TerrainType = "Water", Blocked = true }
            }
        };

        byte[] data = original.Serialize();
        var deserialized = new ChunkDeltaMessage();
        deserialized.Deserialize(data);

        deserialized.ChunkX.ShouldBe(3);
        deserialized.ChunkY.ShouldBe(4);
        deserialized.ChunkSize.ShouldBe(16);
        deserialized.Terrain.Count.ShouldBe(2);
        deserialized.Terrain[0].TerrainType.ShouldBe("Grass");
        deserialized.Terrain[1].Blocked.ShouldBeTrue();
    }

    [Fact]
    public void PredictedPlacementMessage_SerializeDeserialize_RoundTrip_ShouldPreserveAllProperties()
    {
        var original = new PredictedPlacementMessage
        {
            CommandId = 555,
            X = 8,
            Y = 9,
            StructureType = "Farm",
            Rotation = 2,
            IsValid = true,
            ValidationReason = ""
        };

        byte[] data = original.Serialize();
        var deserialized = new PredictedPlacementMessage();
        deserialized.Deserialize(data);

        deserialized.CommandId.ShouldBe(555);
        deserialized.IsValid.ShouldBeTrue();
        deserialized.StructureType.ShouldBe("Farm");
    }

    [Fact]
    public void PredictionReconcileMessage_SerializeDeserialize_RoundTrip_ShouldPreserveServerResult()
    {
        var original = new PredictionReconcileMessage
        {
            CommandId = 777,
            ServerResult = new PlacementResultMessage
            {
                CommandId = 777,
                Success = true,
                ServerTimestamp = 1700000100
            },
            PredictionCorrect = true
        };

        byte[] data = original.Serialize();
        var deserialized = new PredictionReconcileMessage();
        deserialized.Deserialize(data);

        deserialized.CommandId.ShouldBe(777);
        deserialized.PredictionCorrect.ShouldBeTrue();
        deserialized.ServerResult.ShouldNotBeNull();
        deserialized.ServerResult.CommandId.ShouldBe(777);
    }

    [Fact]
    public void MessageTypes_AllValues_ShouldBeUnique()
    {
        var values = new HashSet<byte>
        {
            MessageTypes.PlacementCommand,
            MessageTypes.PlacementResult,
            MessageTypes.PlacementDelta,
            MessageTypes.PlacementRemoved,
            MessageTypes.OccupancyDelta,
            MessageTypes.CellOccupancy,
            MessageTypes.RegionOccupancy,
            MessageTypes.WorldSnapshot,
            MessageTypes.ChunkDelta,
            MessageTypes.PredictedPlacement,
            MessageTypes.PredictionReconcile,
            MessageTypes.ConnectRequest,
            MessageTypes.ConnectResponse,
            MessageTypes.Disconnect
        };

        values.Count.ShouldBe(14);
    }

    [Fact]
    public void AllMessageTypes_ShouldReturnCorrectMessageTypeByte()
    {
        AssertMessageType<PlacementCommandMessage>(MessageTypes.PlacementCommand);
        AssertMessageType<PlacementResultMessage>(MessageTypes.PlacementResult);
        AssertMessageType<PlacementDeltaMessage>(MessageTypes.PlacementDelta);
        AssertMessageType<OccupancyDeltaMessage>(MessageTypes.OccupancyDelta);
        AssertMessageType<CellOccupancyMessage>(MessageTypes.CellOccupancy);
        AssertMessageType<RegionOccupancyMessage>(MessageTypes.RegionOccupancy);
        AssertMessageType<WorldSnapshotMessage>(MessageTypes.WorldSnapshot);
        AssertMessageType<ChunkDeltaMessage>(MessageTypes.ChunkDelta);
        AssertMessageType<PredictedPlacementMessage>(MessageTypes.PredictedPlacement);
        AssertMessageType<PredictionReconcileMessage>(MessageTypes.PredictionReconcile);
        AssertMessageType<ConnectRequestMessage>(MessageTypes.ConnectRequest);
        AssertMessageType<ConnectResponseMessage>(MessageTypes.ConnectResponse);
        AssertMessageType<DisconnectMessage>(MessageTypes.Disconnect);
    }

    private static void AssertMessageType<T>(byte expected) where T : INetworkMessage, new()
    {
        var message = new T();
        message.MessageType.ShouldBe(expected);
    }
}
