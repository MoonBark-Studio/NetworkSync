using MoonBark.Framework.Core;
using MoonBark.NetworkSync.Core.Interfaces;
using MoonBark.NetworkSync.Core.Services;

namespace MoonBark.NetworkSync.Core;

/// <summary>
/// Registers NetworkSync services with the Framework module registry and handles initialization.
/// </summary>
public sealed class NetworkSyncModule : IFrameworkModule, IWorldInitializable
{
    private readonly NetworkManager _networkManager;

    public NetworkSyncModule(NetworkManager networkManager)
    {
        _networkManager = networkManager ?? throw new ArgumentNullException(nameof(networkManager));
    }

    public void ConfigureServices(IServiceRegistry services)
    {
        services.Register(_networkManager);
        services.Register<INetworkTransport>(_networkManager.Transport);
        services.Register<IReplicationService>(_networkManager.ReplicationService);
    }

    public void Initialize(IServiceRegistry services)
    {
        // NetworkManager is fully constructed at this point.
        // Game layer calls ConnectAsync when ready; this hook allows
        // resolving any cross-module dependencies before the network starts.
    }
}
