// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Pipeline;
using Metalama.Framework.DesignTime.Pipeline.Diff;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Pipeline;

/// <summary>
/// Tests for <see cref="SyntaxTreeChange.Merge"/>.
/// </summary>
public sealed class SyntaxTreeChangeMergeTests
{
    private static SyntaxTreeVersion CreateSyntaxTreeVersion( string code, bool hasCompileTimeCode = false, string path = "code.cs" )
    {
        var tree = CSharpSyntaxTree.ParseText( code, path: path );
        var hash = (ulong) code.GetHashCodeOrdinal();

        return new SyntaxTreeVersion( tree, hasCompileTimeCode, hash, ImmutableArray<Framework.DesignTime.Pipeline.Dependencies.TypeDependencyKey>.Empty );
    }

    private static SyntaxTreeChange CreateChange(
        SyntaxTreeChangeKind changeKind,
        CompileTimeChangeKind compileTimeChangeKind,
        in SyntaxTreeVersion oldVersion,
        in SyntaxTreeVersion newVersion,
        string path = "code.cs" )
        => new( path, changeKind, compileTimeChangeKind, oldVersion, newVersion );

    // --- SyntaxTreeChangeKind transition tests ---

    [Fact]
    public void Merge_Added_Then_Changed_Produces_Added()
    {
        var newVersion = CreateSyntaxTreeVersion( "class C { }" );
        var changedVersion = CreateSyntaxTreeVersion( "class C { int X; }" );

        var first = CreateChange( SyntaxTreeChangeKind.Added, CompileTimeChangeKind.None, default, newVersion );
        var second = CreateChange( SyntaxTreeChangeKind.Changed, CompileTimeChangeKind.None, newVersion, changedVersion );

        var merged = first.Merge( second );

        Assert.Equal( SyntaxTreeChangeKind.Added, merged.SyntaxTreeChangeKind );
    }

    [Fact]
    public void Merge_Added_Then_Removed_Produces_None()
    {
        var newVersion = CreateSyntaxTreeVersion( "class C { }" );

        var first = CreateChange( SyntaxTreeChangeKind.Added, CompileTimeChangeKind.None, default, newVersion );
        var second = CreateChange( SyntaxTreeChangeKind.Removed, CompileTimeChangeKind.None, newVersion, default );

        var merged = first.Merge( second );

        Assert.Equal( SyntaxTreeChangeKind.None, merged.SyntaxTreeChangeKind );
    }

    [Fact]
    public void Merge_Added_Then_Added_Produces_Added()
    {
        var newVersion = CreateSyntaxTreeVersion( "class C { }" );
        var readdedVersion = CreateSyntaxTreeVersion( "class C { int X; }" );

        var first = CreateChange( SyntaxTreeChangeKind.Added, CompileTimeChangeKind.None, default, newVersion );
        var second = CreateChange( SyntaxTreeChangeKind.Added, CompileTimeChangeKind.None, default, readdedVersion );

        var merged = first.Merge( second );

        Assert.Equal( SyntaxTreeChangeKind.Added, merged.SyntaxTreeChangeKind );
    }

    [Fact]
    public void Merge_Changed_Then_Changed_Produces_Changed()
    {
        var oldVersion = CreateSyntaxTreeVersion( "class C { }" );
        var intermediateVersion = CreateSyntaxTreeVersion( "class C { int X; }" );
        var finalVersion = CreateSyntaxTreeVersion( "class C { int X; int Y; }" );

        var first = CreateChange( SyntaxTreeChangeKind.Changed, CompileTimeChangeKind.None, oldVersion, intermediateVersion );
        var second = CreateChange( SyntaxTreeChangeKind.Changed, CompileTimeChangeKind.None, intermediateVersion, finalVersion );

        var merged = first.Merge( second );

        Assert.Equal( SyntaxTreeChangeKind.Changed, merged.SyntaxTreeChangeKind );
    }

    [Fact]
    public void Merge_Changed_Then_Removed_Produces_Removed()
    {
        var oldVersion = CreateSyntaxTreeVersion( "class C { }" );
        var changedVersion = CreateSyntaxTreeVersion( "class C { int X; }" );

        var first = CreateChange( SyntaxTreeChangeKind.Changed, CompileTimeChangeKind.None, oldVersion, changedVersion );
        var second = CreateChange( SyntaxTreeChangeKind.Removed, CompileTimeChangeKind.None, changedVersion, default );

        var merged = first.Merge( second );

        Assert.Equal( SyntaxTreeChangeKind.Removed, merged.SyntaxTreeChangeKind );
    }

    [Fact]
    public void Merge_Changed_Then_Added_DifferentHash_Produces_Changed()
    {
        // Regression test for https://github.com/metalama/Metalama/issues/630
        var oldVersion = CreateSyntaxTreeVersion( "class C { }" );
        var intermediateVersion = CreateSyntaxTreeVersion( "class C { int X; }" );
        var newVersion = CreateSyntaxTreeVersion( "class C { int X; int Y; }" );

        var first = CreateChange( SyntaxTreeChangeKind.Changed, CompileTimeChangeKind.None, oldVersion, intermediateVersion );
        var second = CreateChange( SyntaxTreeChangeKind.Added, CompileTimeChangeKind.None, default, newVersion );

        var merged = first.Merge( second );

        Assert.Equal( SyntaxTreeChangeKind.Changed, merged.SyntaxTreeChangeKind );
    }

    [Fact]
    public void Merge_Changed_Then_Added_SameHash_Produces_None()
    {
        var oldVersion = CreateSyntaxTreeVersion( "class C { }" );
        var intermediateVersion = CreateSyntaxTreeVersion( "class C { int X; }" );
        var revertedVersion = CreateSyntaxTreeVersion( "class C { }" );

        var first = CreateChange( SyntaxTreeChangeKind.Changed, CompileTimeChangeKind.None, oldVersion, intermediateVersion );
        var second = CreateChange( SyntaxTreeChangeKind.Added, CompileTimeChangeKind.None, default, revertedVersion );

        var merged = first.Merge( second );

        Assert.Equal( SyntaxTreeChangeKind.None, merged.SyntaxTreeChangeKind );
    }

    [Fact]
    public void Merge_Removed_Then_Added_DifferentHash_Produces_Changed()
    {
        var oldVersion = CreateSyntaxTreeVersion( "class C { }" );
        var newVersion = CreateSyntaxTreeVersion( "class C { int X; }" );

        var first = CreateChange( SyntaxTreeChangeKind.Removed, CompileTimeChangeKind.None, oldVersion, default );
        var second = CreateChange( SyntaxTreeChangeKind.Added, CompileTimeChangeKind.None, default, newVersion );

        var merged = first.Merge( second );

        Assert.Equal( SyntaxTreeChangeKind.Changed, merged.SyntaxTreeChangeKind );
    }

    [Fact]
    public void Merge_Removed_Then_Added_SameHash_Produces_None()
    {
        var oldVersion = CreateSyntaxTreeVersion( "class C { }" );
        var restoredVersion = CreateSyntaxTreeVersion( "class C { }" );

        var first = CreateChange( SyntaxTreeChangeKind.Removed, CompileTimeChangeKind.None, oldVersion, default );
        var second = CreateChange( SyntaxTreeChangeKind.Added, CompileTimeChangeKind.None, default, restoredVersion );

        var merged = first.Merge( second );

        Assert.Equal( SyntaxTreeChangeKind.None, merged.SyntaxTreeChangeKind );
    }

    [Fact]
    public void Merge_Removed_Then_Changed_Produces_Changed()
    {
        var oldVersion = CreateSyntaxTreeVersion( "class C { }" );
        var changedVersion = CreateSyntaxTreeVersion( "class C { int X; }" );

        var first = CreateChange( SyntaxTreeChangeKind.Removed, CompileTimeChangeKind.None, oldVersion, default );
        var second = CreateChange( SyntaxTreeChangeKind.Changed, CompileTimeChangeKind.None, default, changedVersion );

        var merged = first.Merge( second );

        Assert.Equal( SyntaxTreeChangeKind.Changed, merged.SyntaxTreeChangeKind );
    }

    [Fact]
    public void Merge_Removed_Then_Removed_Produces_Removed()
    {
        var oldVersion = CreateSyntaxTreeVersion( "class C { }" );

        var first = CreateChange( SyntaxTreeChangeKind.Removed, CompileTimeChangeKind.None, oldVersion, default );
        var second = CreateChange( SyntaxTreeChangeKind.Removed, CompileTimeChangeKind.None, default, default );

        var merged = first.Merge( second );

        Assert.Equal( SyntaxTreeChangeKind.Removed, merged.SyntaxTreeChangeKind );
    }

    // --- CompileTimeChangeKind transition tests ---

    [Fact]
    public void Merge_CompileTime_None_Then_NewlyCompileTime()
    {
        var oldVersion = CreateSyntaxTreeVersion( "class C { }" );
        var intermediateVersion = CreateSyntaxTreeVersion( "class C { int X; }" );
        var finalVersion = CreateSyntaxTreeVersion( "class C { int X; int Y; }", hasCompileTimeCode: true );

        var first = CreateChange( SyntaxTreeChangeKind.Changed, CompileTimeChangeKind.None, oldVersion, intermediateVersion );
        var second = CreateChange( SyntaxTreeChangeKind.Changed, CompileTimeChangeKind.NewlyCompileTime, intermediateVersion, finalVersion );

        var merged = first.Merge( second );

        Assert.Equal( CompileTimeChangeKind.NewlyCompileTime, merged.CompileTimeChangeKind );
    }

    [Fact]
    public void Merge_CompileTime_None_Then_NoLongerCompileTime()
    {
        var oldVersion = CreateSyntaxTreeVersion( "class C { }" );
        var intermediateVersion = CreateSyntaxTreeVersion( "class C { int X; }" );
        var finalVersion = CreateSyntaxTreeVersion( "class C { int X; int Y; }" );

        var first = CreateChange( SyntaxTreeChangeKind.Changed, CompileTimeChangeKind.None, oldVersion, intermediateVersion );
        var second = CreateChange( SyntaxTreeChangeKind.Changed, CompileTimeChangeKind.NoLongerCompileTime, intermediateVersion, finalVersion );

        var merged = first.Merge( second );

        Assert.Equal( CompileTimeChangeKind.NoLongerCompileTime, merged.CompileTimeChangeKind );
    }

    [Fact]
    public void Merge_CompileTime_NewlyCompileTime_Then_None()
    {
        var oldVersion = CreateSyntaxTreeVersion( "class C { }" );
        var intermediateVersion = CreateSyntaxTreeVersion( "class C { int X; }", hasCompileTimeCode: true );
        var finalVersion = CreateSyntaxTreeVersion( "class C { int X; int Y; }", hasCompileTimeCode: true );

        var first = CreateChange( SyntaxTreeChangeKind.Changed, CompileTimeChangeKind.NewlyCompileTime, oldVersion, intermediateVersion );
        var second = CreateChange( SyntaxTreeChangeKind.Changed, CompileTimeChangeKind.None, intermediateVersion, finalVersion );

        var merged = first.Merge( second );

        Assert.Equal( CompileTimeChangeKind.NewlyCompileTime, merged.CompileTimeChangeKind );
    }

    [Fact]
    public void Merge_CompileTime_NewlyCompileTime_Then_NoLongerCompileTime_Produces_None()
    {
        var oldVersion = CreateSyntaxTreeVersion( "class C { }" );
        var intermediateVersion = CreateSyntaxTreeVersion( "class C { int X; }", hasCompileTimeCode: true );
        var finalVersion = CreateSyntaxTreeVersion( "class C { int X; int Y; }" );

        var first = CreateChange( SyntaxTreeChangeKind.Changed, CompileTimeChangeKind.NewlyCompileTime, oldVersion, intermediateVersion );
        var second = CreateChange( SyntaxTreeChangeKind.Changed, CompileTimeChangeKind.NoLongerCompileTime, intermediateVersion, finalVersion );

        var merged = first.Merge( second );

        Assert.Equal( CompileTimeChangeKind.None, merged.CompileTimeChangeKind );
    }

    [Fact]
    public void Merge_CompileTime_NewlyCompileTime_Then_NewlyCompileTime()
    {
        var oldVersion = CreateSyntaxTreeVersion( "class C { }" );
        var intermediateVersion = CreateSyntaxTreeVersion( "class C { int X; }", hasCompileTimeCode: true );
        var finalVersion = CreateSyntaxTreeVersion( "class C { int X; int Y; }", hasCompileTimeCode: true );

        var first = CreateChange( SyntaxTreeChangeKind.Changed, CompileTimeChangeKind.NewlyCompileTime, oldVersion, intermediateVersion );
        var second = CreateChange( SyntaxTreeChangeKind.Changed, CompileTimeChangeKind.NewlyCompileTime, intermediateVersion, finalVersion );

        var merged = first.Merge( second );

        Assert.Equal( CompileTimeChangeKind.NewlyCompileTime, merged.CompileTimeChangeKind );
    }

    [Fact]
    public void Merge_CompileTime_NoLongerCompileTime_Then_NewlyCompileTime_Produces_None()
    {
        var oldVersion = CreateSyntaxTreeVersion( "class C { }", hasCompileTimeCode: true );
        var intermediateVersion = CreateSyntaxTreeVersion( "class C { int X; }" );
        var finalVersion = CreateSyntaxTreeVersion( "class C { int X; int Y; }", hasCompileTimeCode: true );

        var first = CreateChange( SyntaxTreeChangeKind.Changed, CompileTimeChangeKind.NoLongerCompileTime, oldVersion, intermediateVersion );
        var second = CreateChange( SyntaxTreeChangeKind.Changed, CompileTimeChangeKind.NewlyCompileTime, intermediateVersion, finalVersion );

        var merged = first.Merge( second );

        Assert.Equal( CompileTimeChangeKind.None, merged.CompileTimeChangeKind );
    }

    [Fact]
    public void Merge_CompileTime_NoLongerCompileTime_Then_NoLongerCompileTime()
    {
        var oldVersion = CreateSyntaxTreeVersion( "class C { }", hasCompileTimeCode: true );
        var intermediateVersion = CreateSyntaxTreeVersion( "class C { int X; }" );
        var finalVersion = CreateSyntaxTreeVersion( "class C { int X; int Y; }" );

        var first = CreateChange( SyntaxTreeChangeKind.Changed, CompileTimeChangeKind.NoLongerCompileTime, oldVersion, intermediateVersion );
        var second = CreateChange( SyntaxTreeChangeKind.Changed, CompileTimeChangeKind.NoLongerCompileTime, intermediateVersion, finalVersion );

        var merged = first.Merge( second );

        Assert.Equal( CompileTimeChangeKind.NoLongerCompileTime, merged.CompileTimeChangeKind );
    }
}
