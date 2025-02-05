// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.DesignTime.Rpc;
using System.Collections.Immutable;

// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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