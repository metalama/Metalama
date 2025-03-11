// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.Engine;
using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.VisualStudio.ServiceProvider;

internal sealed partial class RpcServiceProviderService : RpcService<IRpcServiceProviderApi>
{
    private ImmutableArray<RpcServiceInfo> _registeredServices;

    public RpcServiceProviderService( RpcServiceProviderServerEndpoint serverEndpoint, IEnumerable<Type> serviceFactoryTypes ) : base( serverEndpoint )
    {
        this._registeredServices = serviceFactoryTypes.Select( t => new RpcServiceInfo( serverEndpoint.PipeName, t.AssemblyQualifiedName.AssertNotNull() ) )
            .ToImmutableArray();
    }

    protected override IRpcServiceProviderApi CreateApi( IRpcEventSender eventSender ) => new Api( this );

    public Task AddServicesAsync( string pipeName, IEnumerable<IRpcServiceFactory> serviceFactories, CancellationToken cancellationToken )
    {
        var serviceInfo = serviceFactories.Select( t => new RpcServiceInfo( pipeName, t.GetType().AssemblyQualifiedName.AssertNotNull() ) ).ToImmutableArray();

        this._registeredServices = this._registeredServices.AddRange( serviceInfo );

        return this.RaiseEventAsync( new ServicesAddedEventData( serviceInfo ), cancellationToken );
    }
}