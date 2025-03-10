// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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