using MoonBark.NetworkSync.Core.Transports;
using Shouldly;
using Xunit;

namespace MoonBark.NetworkSync.Tests.StressTests;

public sealed class LiteNetTransportTests
{
    [Fact]
    public void GameVersion_WhenSetToNull_ShouldFallbackToDefault()
    {
        var transport = new LiteNetTransport();

        transport.GameVersion = null!;

        transport.GameVersion.ShouldBe("1.0.0");
    }

    [Fact]
    public void GameVersion_WhenSetToValidString_ShouldUseThatValue()
    {
        var transport = new LiteNetTransport();

        transport.GameVersion = "2.5.0";

        transport.GameVersion.ShouldBe("2.5.0");
    }

    [Fact]
    public void IsConnected_WhenNotStarted_ShouldBeFalse()
    {
        var transport = new LiteNetTransport();

        transport.IsConnected.ShouldBeFalse();
    }

    [Fact]
    public void ConnectionState_WhenNotStarted_ShouldBeDisconnected()
    {
        var transport = new LiteNetTransport();

        transport.ConnectionState.ShouldBe(NetworkSync.Core.Interfaces.NetworkConnectionState.Disconnected);
    }

    [Fact]
    public void IsServer_WhenNotStarted_ShouldBeFalse()
    {
        var transport = new LiteNetTransport();

        transport.IsServer.ShouldBeFalse();
    }
}
