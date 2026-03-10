// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Pipeline;
using Metalama.Framework.DesignTime.Pipeline.Diff;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Pipeline;

/// <summary>
/// Tests for <see cref="SyntaxTreeChange.Merge"/>.
/// </summary>
public sealed class SyntaxTreeChangeMergeTests
{
    private static SyntaxTreeVersion CreateSyntaxTreeVersion( string code, string path = "code.cs" )
    {
        var tree = CSharpSyntaxTree.ParseText( code, path: path );
        var hash = (ulong) code.GetHashCode( System.StringComparison.Ordinal );

        return new SyntaxTreeVersion( tree, false, hash, ImmutableArray<Framework.DesignTime.Pipeline.Dependencies.TypeDependencyKey>.Empty );
    }

    [Fact]
    public void MergeChangedAndAdded()
    {
        // Regression test for https://github.com/metalama/Metalama/issues/630
        // This combination can occur during merging of referenced project changes
        // when the same file appears as Changed in the first diff and Added in the second diff.

        var oldVersion = CreateSyntaxTreeVersion( "class C {}", "code.cs" );
        var intermediateVersion = CreateSyntaxTreeVersion( "class C { int X; }", "code.cs" );
        var newVersion = CreateSyntaxTreeVersion( "class C { int X; int Y; }", "code.cs" );

        var firstChange = new SyntaxTreeChange(
            "code.cs",
            SyntaxTreeChangeKind.Changed,
            CompileTimeChangeKind.None,
            oldVersion,
            intermediateVersion );

        var secondChange = new SyntaxTreeChange(
            "code.cs",
            SyntaxTreeChangeKind.Added,
            CompileTimeChangeKind.None,
            default,
            newVersion );

        // This should not throw. The merged result should be Changed (file existed in original and exists in final).
        var merged = firstChange.Merge( secondChange );

        Assert.Equal( SyntaxTreeChangeKind.Changed, merged.SyntaxTreeChangeKind );
        Assert.Equal( "code.cs", merged.FilePath );
    }

    [Fact]
    public void MergeChangedAndAddedWithSameHash()
    {
        // When the final content is the same as the original, the merge should produce None.

        var oldVersion = CreateSyntaxTreeVersion( "class C {}", "code.cs" );
        var intermediateVersion = CreateSyntaxTreeVersion( "class C { int X; }", "code.cs" );
        var newVersion = CreateSyntaxTreeVersion( "class C {}", "code.cs" );

        var firstChange = new SyntaxTreeChange(
            "code.cs",
            SyntaxTreeChangeKind.Changed,
            CompileTimeChangeKind.None,
            oldVersion,
            intermediateVersion );

        var secondChange = new SyntaxTreeChange(
            "code.cs",
            SyntaxTreeChangeKind.Added,
            CompileTimeChangeKind.None,
            default,
            newVersion );

        var merged = firstChange.Merge( secondChange );

        // The file content went back to the original, so the net change is None.
        Assert.Equal( SyntaxTreeChangeKind.None, merged.SyntaxTreeChangeKind );
    }
}
