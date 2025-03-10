// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Newtonsoft.Json;
using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.Rpc.Notifications;

[JsonObject]
public class CompilationResultChangedEventData : RpcEventData
{
    public ProjectKey ProjectKey { get; }

    public bool IsPartialCompilation { get; }

    public ImmutableArray<string> SyntaxTreePaths { get; }

    public CompilationResultChangedEventData( ProjectKey projectKey, bool isPartialCompilation, ImmutableArray<string> syntaxTreePaths )
    {
        this.ProjectKey = projectKey;
        this.IsPartialCompilation = isPartialCompilation;
        this.SyntaxTreePaths = syntaxTreePaths.IsDefault ? ImmutableArray<string>.Empty : syntaxTreePaths;
    }

    public override string Category => "";
}