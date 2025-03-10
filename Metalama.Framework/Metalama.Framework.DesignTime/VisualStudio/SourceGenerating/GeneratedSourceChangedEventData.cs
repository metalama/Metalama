// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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