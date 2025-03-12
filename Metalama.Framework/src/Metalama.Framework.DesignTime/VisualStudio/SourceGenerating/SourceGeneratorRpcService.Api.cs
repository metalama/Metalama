// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.DesignTime.Rpc;
using System.Collections.Immutable;

// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.DesignTime.VisualStudio.SourceGenerating;

internal sealed partial class SourceGeneratorRpcService
{
    private sealed class Api : ISourceGeneratorRpcApi
    {
        private readonly SourceGeneratorRpcService _parent;

        public Api( SourceGeneratorRpcService parent )
        {
            this._parent = parent;
        }

        public Task RegisterAsync( ProjectKey projectKey, CancellationToken cancellationToken )
        {
            this._parent.ClientConnected?.Invoke( projectKey );

            return Task.CompletedTask;
        }

        public Task<ImmutableDictionary<string, string>> GetGeneratedSourcesAsync( ProjectKey projectKey, [UsedImplicitly] CancellationToken cancellationToken )
        {
            if ( !this._parent._generatedSourcesCache.TryGetValue( projectKey, out var sources ) )
            {
                sources = ImmutableDictionary<string, string>.Empty;
            }

            return Task.FromResult( sources );
        }
    }
}