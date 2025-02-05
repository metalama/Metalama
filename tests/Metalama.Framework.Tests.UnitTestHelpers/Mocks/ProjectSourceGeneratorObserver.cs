// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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