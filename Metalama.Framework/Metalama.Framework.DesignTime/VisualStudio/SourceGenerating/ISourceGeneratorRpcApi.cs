// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.DesignTime.Rpc;
using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.VisualStudio.SourceGenerating;

internal interface ISourceGeneratorRpcApi : IRpcApi
{
    Task RegisterAsync( ProjectKey projectKey, [UsedImplicitly] CancellationToken cancellationToken );

    Task<ImmutableDictionary<string, string>> GetGeneratedSourcesAsync( ProjectKey projectKey, [UsedImplicitly] CancellationToken cancellationToken );
}