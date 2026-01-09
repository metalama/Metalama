// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Utilities;

public sealed class RemovePreprocessorDirectivesRewriterTests
{
    [Fact]
    public void RemovesActiveIfDirective()
    {
        const string code = """
            #if !BENCHMARK
            class TestClass { }
            #endif
            """;

        var tree = CSharpSyntaxTree.ParseText( code );
        var root = tree.GetRoot();

        var rewriter = new RemovePreprocessorDirectivesRewriter();
        var rewritten = rewriter.Visit( root );
        var result = rewritten!.ToFullString();

        // Verify the preprocessor directives were removed
        Assert.DoesNotContain( "#if", result );
        Assert.DoesNotContain( "#endif", result );
        Assert.Contains( "class TestClass", result );
    }

    [Fact]
    public void RemovesActiveIfDirectiveWithUsings()
    {
        const string code = """
            #if !BENCHMARK
            using System;

            class TestClass { }
            #endif
            """;

        var tree = CSharpSyntaxTree.ParseText( code );
        var root = tree.GetRoot();

        var rewriter = new RemovePreprocessorDirectivesRewriter();
        var rewritten = rewriter.Visit( root );
        var result = rewritten!.ToFullString();

        // Verify the preprocessor directives were removed
        Assert.DoesNotContain( "#if", result );
        Assert.DoesNotContain( "#endif", result );
        Assert.Contains( "using System", result );
        Assert.Contains( "class TestClass", result );
    }

    [Fact]
    public void RemovesInactiveCode()
    {
        const string code = """
            class Active1 { }

            #if UNDEFINED_SYMBOL
            class Inactive { }
            #endif

            class Active2 { }
            """;

        var tree = CSharpSyntaxTree.ParseText( code );
        var root = tree.GetRoot();

        var rewriter = new RemovePreprocessorDirectivesRewriter();
        var rewritten = rewriter.Visit( root );
        var result = rewritten!.ToFullString();

        // Verify the preprocessor directives were removed
        Assert.DoesNotContain( "#if", result );
        Assert.DoesNotContain( "#endif", result );
        Assert.Contains( "class Active1", result );
        Assert.DoesNotContain( "class Inactive", result );
        Assert.Contains( "class Active2", result );
    }

    [Fact]
    public void HandlesNestedDirectives()
    {
        const string code = """
            #if !BENCHMARK
            class Outer { }
            #if DEBUG
            class Inner { }
            #endif
            class AfterInner { }
            #endif
            """;

        var tree = CSharpSyntaxTree.ParseText( code );
        var root = tree.GetRoot();

        var rewriter = new RemovePreprocessorDirectivesRewriter();
        var rewritten = rewriter.Visit( root );
        var result = rewritten!.ToFullString();

        // Verify the preprocessor directives were removed
        Assert.DoesNotContain( "#if", result );
        Assert.DoesNotContain( "#endif", result );
        Assert.Contains( "class Outer", result );
        Assert.Contains( "class AfterInner", result );
    }
}
