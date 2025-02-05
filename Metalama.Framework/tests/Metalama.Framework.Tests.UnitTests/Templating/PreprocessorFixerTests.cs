// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Templating;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Templating;

public sealed class PreprocessorFixerTests : UnitTestClass
{
    [Fact]
    public void DisabledText()
    {
        const string code = """
                            static class Program
                            {
                                static void M()
                                {
                            #if DEBUG
                                    Console.WriteLine();
                            #endif
                                }
                            }
                            """;

        var node = SyntaxFactory.ParseCompilationUnit( code );

        var generationContext = new SyntaxGenerationContext( compilationContext: null!, isNullOblivious: false, isPartial: false, SyntaxGenerationOptions.Formatted, "\r\n" );

        var fixedNode = PreprocessorFixer.Fix( node, generationContext );

        Assert.Same( node, fixedNode );
    }

    [Fact]
    public void EmptyIfDirective()
    {
        const string code = """
                            class C
                            {
                            	[Foo]
                            #if NET5_0_OR_GREATER
                            #endif
                            	void M()
                            	{
                            	}
                            }
                            """;

        var node = SyntaxFactory.ParseCompilationUnit( code );

        var generationContext = new SyntaxGenerationContext( compilationContext: null!, isNullOblivious: false, isPartial: false, SyntaxGenerationOptions.Formatted, "\r\n" );

        var fixedNode = PreprocessorFixer.Fix( node, generationContext );

        Assert.Same( node, fixedNode );
    }
}
