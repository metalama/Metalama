// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using K4os.Hash.xxHash;
using Metalama.Framework.DesignTime.Pipeline.Diff;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.Utilities;

public sealed class HasherTests
{
    private readonly ITestOutputHelper _testOutput;

    public HasherTests( ITestOutputHelper testOutput )
    {
        this._testOutput = testOutput;
    }

    [Theory]

    // Expression body.
    [InlineData( "class C { int M() => 5; }", "class C { int M() => 6; }", false, true )]

    // Statement body.
    [InlineData( "class C { int M() { return 5; } }", "class C { int M() { return 6; } }", false, true )]

    // Member name.
    [InlineData( "class C { int M() { return 5; } }", "class C { int N() { return 5; } }", false, false )]

    // Trivia.
    [InlineData( "class C { int M() { return 5; } } /* Trivia */", "class C { int M() { return 5; } }", true, true )]
    public void IsEqual( string code1, string code2, bool shouldBeEqualCompileTime, bool shouldBeEqualRunTime )
    {
        var xxh = new XXH64();

        var tree1 = CSharpSyntaxTree.ParseText( code1, SupportedCSharpVersions.DefaultParseOptions );
        var tree2 = CSharpSyntaxTree.ParseText( code2, SupportedCSharpVersions.DefaultParseOptions );

        var compileTimeHash1 = HashCompileTime( tree1 );
        var compileTimeHash2 = HashCompileTime( tree2 );

        Assert.Equal( shouldBeEqualCompileTime, compileTimeHash1 == compileTimeHash2 );

        var runTimeHash1 = HashRunTime( tree1 );
        var runTimeHash2 = HashRunTime( tree2 );

        Assert.Equal( shouldBeEqualRunTime, runTimeHash1 == runTimeHash2 );

        ulong HashCompileTime( SyntaxTree tree )
        {
            xxh.Reset();

            var hasher = new CompileTimeCodeHasher( xxh );
            hasher.EnableLogging();
            hasher.Visit( tree.GetRoot() );

            this._testOutput.WriteLine( hasher.Log!.ToString() );
            this._testOutput.WriteLine( "---" );

            return xxh.Digest();
        }

        ulong HashRunTime( SyntaxTree tree )
        {
            xxh.Reset();
            var hasher = new RunTimeCodeHasher( xxh );
            hasher.Visit( tree.GetRoot() );

            return xxh.Digest();
        }
    }
}