// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Introspection.Navigation.Links;
using Metalama.Mcp.Worker;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using Xunit;

namespace Metalama.Mcp.Tests;

public sealed class WorkspaceKeyTests
{
    private static readonly string _root = Path.Combine( Path.GetTempPath(), "metalama-mcp-key-tests" );

    private static ImmutableArray<string> Paths( params string[] relativePaths )
        => [..System.Linq.Enumerable.Select( relativePaths, p => Path.GetFullPath( Path.Combine( _root, p ) ) )];

    private static readonly Dictionary<string, ImmutableArray<string>> _none = new();

    [Fact]
    public void KeyIsTheFileNameWhereverTheFileIs()
    {
        // A path relative to the working directory, such as ../../src/App.sln, made links full of %2F.
        Assert.Equal( "App.sln", WorkspaceRegistry.GetKey( Paths( "../../src/App.sln" ), _none ) );
    }

    [Fact]
    public void KeyOfSeveralPathsJoinsTheirFileNames()
    {
        Assert.Equal( "A.csproj+B.csproj", WorkspaceRegistry.GetKey( Paths( "a/A.csproj", "b/B.csproj" ), _none ) );
    }

    [Fact]
    public void KeyThatIsTakenByOtherPathsGetsASuffix()
    {
        var loaded = new Dictionary<string, ImmutableArray<string>> { ["App.sln"] = Paths( "one/App.sln" ) };

        var key = WorkspaceRegistry.GetKey( Paths( "two/App.sln" ), loaded );

        Assert.Matches( "^App\\.sln~[0-9a-f]{6}$", key );
        Assert.Equal( key, WorkspaceRegistry.GetKey( Paths( "two/App.sln" ), loaded ) );
    }

    [Fact]
    public void KeyOfTheSamePathsIsReused()
    {
        var loaded = new Dictionary<string, ImmutableArray<string>> { ["App.sln"] = Paths( "one/App.sln" ) };

        Assert.Equal( "App.sln", WorkspaceRegistry.GetKey( Paths( "one/App.sln" ), loaded ) );
    }

    [Fact]
    public void LinkBaseHasNoEscapedSeparator()
    {
        var key = WorkspaceRegistry.GetKey( Paths( "../../src/App.sln" ), _none );

        Assert.Equal( "metalama://App.sln/App@net10.0/source/", new LinkBase( key, "App@net10.0" ).ToString() );
    }
}
