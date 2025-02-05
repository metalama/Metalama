// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.VisualStudio.ServiceProvider;

internal sealed partial class RpcServiceProviderService
{
    private sealed class Api : IRpcServiceProviderApi
    {
        private readonly RpcServiceProviderService _parent;

        public Api( RpcServiceProviderService parent )
        {
            this._parent = parent;
        }

        public Task<ImmutableArray<RpcServiceInfo>> GetRegisteredServicesAsync( CancellationToken cancellationToken )
            => Task.FromResult( this._parent._registeredServices.ToImmutableArray() );
    }
}