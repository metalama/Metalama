// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.DesignTime.Rpc.Notifications;
using Metalama.Framework.DesignTime.VisualStudio.Rpc;
using Metalama.Framework.Engine.Services;

namespace Metalama.Framework.DesignTime.VisualStudio.ServiceHub;

/// <summary>
/// The <see cref="ServiceHubServerEndpoint"/> class resides in the DevEnv UI process. It is a singleton instance (one per Metalama version)
/// that awaits connections from Roslyn analysis processes . The name of the pipe can be deterministically computed by
/// the Roslyn process. Also, it hosts the <see cref="EventHub"/>, allowing to subscribe to messages of any service in the hub.
/// </summary>
public sealed class ServiceHubServerEndpoint : ServerEndpoint, IServiceHubRpcServiceProvider
{
    private static readonly object _initializeLock = new();
    private static ServiceHubServerEndpoint? _instance;

    internal ServiceHubServerEndpoint( GlobalServiceProvider serviceProvider, string pipeName ) : base(
        serviceProvider.Underlying,
        pipeName )
    {
        this.ServiceHub = new ServiceHubRpcService( serviceProvider, this );
        this.EventHub = new EventHubRpcService( this );
        this.ServiceHub.EventReceived += this.OnEventReceived;
    }

    private void OnEventReceived( RpcEventData eventData )
    {
        // TODO: Exception handling.
        _ = this.EventHub.ForwardEventAsync( eventData, CancellationToken.None );
    }

    public ServiceHubRpcService ServiceHub { get; }

    internal EventHubRpcService EventHub { get; }

    protected override IEnumerable<RpcService> CreateServices() => [this.ServiceHub, this.EventHub];

    internal static ServiceHubServerEndpoint GetInstance( GlobalServiceProvider serviceProvider )
    {
        if ( _instance == null )
        {
            lock ( _initializeLock )
            {
                if ( _instance == null )
                {
                    var pipeName = PipeNameProvider.GetPipeName( EndpointRole.Discovery );
                    _instance = new ServiceHubServerEndpoint( serviceProvider, pipeName );
                    _instance.Start();
                }
            }
        }

        return _instance;
    }

    protected override void Dispose( bool disposing )
    {
        base.Dispose( disposing );
        this.ServiceHub.EventReceived -= this.OnEventReceived;
    }
}