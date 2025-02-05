// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.DesignTime.Rpc;
using Newtonsoft.Json;
using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.VisualStudio.SourceGenerating;

[JsonObject]
internal sealed class GeneratedSourceChangedEventData : RpcEventData
{
    public ProjectKey ProjectKey { get; }

    public ImmutableDictionary<string, string> Sources { get; }

    public GeneratedSourceChangedEventData( ProjectKey projectKey, ImmutableDictionary<string, string> sources )
    {
        this.ProjectKey = projectKey;
        this.Sources = sources;
    }

    public override string Category => nameof(ISourceGeneratorRpcApi);
}