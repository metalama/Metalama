// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.DesignTime.SourceGeneration;
using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Metalama.Framework.Tests.UnitTestHelpers.Mocks;

public sealed class ProjectSourceGeneratorObserver : IProjectSourceGeneratorObserver
{
    public BlockingCollection<(ProjectKey ProjectKey, ImmutableDictionary<string, string> Sources)> PublishedSources { get; } = new();

    public BlockingCollection<(ProjectKey ProjectKey, string Content)> PublishedTouchFiles { get; } = new();

    public void OnGeneratedCodePublished( ProjectKey projectKey, ImmutableDictionary<string, string> sources )
        => this.PublishedSources.Add( (projectKey, sources) );

    public void OnTouchFileWritten( ProjectKey projectKey, string content ) => this.PublishedTouchFiles.Add( (projectKey, content) );

    public void Reset()
    {
        while ( this.PublishedTouchFiles.TryTake( out _ ) ) { }

        while ( this.PublishedSources.TryTake( out _ ) ) { }
    }
}