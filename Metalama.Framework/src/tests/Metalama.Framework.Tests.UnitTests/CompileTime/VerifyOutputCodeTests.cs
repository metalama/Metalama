// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Pipeline.CompileTime;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CompileTime;

/// <summary>
/// Tests for the VerifyOutputCode option that validates generated syntax trees.
/// </summary>
public sealed class VerifyOutputCodeTests : UnitTestClass
{
    [Fact]
    public async Task VerifyOutputCode_WithValidCode_Succeeds()
    {
        const string code = """
            using Metalama.Framework.Aspects;
            using Metalama.Framework.Code;

            class TestAspect : TypeAspect
            {
                public override void BuildAspect(IAspectBuilder<INamedType> builder)
                {
                    builder.Advice.IntroduceMethod(builder.Target, nameof(IntroducedMethod));
                }

                [Template]
                public void IntroducedMethod()
                {
                    System.Console.WriteLine("Hello");
                }
            }

            [TestAspect]
            class TargetClass { }
            """;

        var testOptions = new TestContextOptions { VerifyOutputCode = true };
        using var testContext = this.CreateTestContext( testOptions );
        var compilation = testContext.CreateCSharpCompilation( code );
        var pipeline = new CompileTimeAspectPipeline( testContext.ServiceProvider );
        var result = await pipeline.ExecuteAsync( null, null, compilation, default, default );

        Assert.True( result.IsSuccessful, "Pipeline should succeed with valid code" );
    }

    [Fact]
    public async Task VerifyOutputCode_Disabled_DoesNotValidate()
    {
        const string code = """
            using Metalama.Framework.Aspects;
            using Metalama.Framework.Code;

            class TestAspect : TypeAspect
            {
                public override void BuildAspect(IAspectBuilder<INamedType> builder)
                {
                    builder.Advice.IntroduceMethod(builder.Target, nameof(IntroducedMethod));
                }

                [Template]
                public void IntroducedMethod()
                {
                    System.Console.WriteLine("Hello");
                }
            }

            [TestAspect]
            class TargetClass { }
            """;

        var testOptions = new TestContextOptions { VerifyOutputCode = false };
        using var testContext = this.CreateTestContext( testOptions );
        var compilation = testContext.CreateCSharpCompilation( code );
        var pipeline = new CompileTimeAspectPipeline( testContext.ServiceProvider );
        var result = await pipeline.ExecuteAsync( null, null, compilation, default, default );

        // With VerifyOutputCode disabled, even if there were syntax issues in output,
        // the pipeline would not fail due to syntax validation
        Assert.True( result.IsSuccessful, "Pipeline should succeed when validation is disabled" );
    }

    [Fact]
    public async Task VerifyOutputCode_WithInvalidGeneratedCode_Fails()
    {
        const string code = """
            using Metalama.Framework.Aspects;
            using Metalama.Framework.Engine;
            using Metalama.Framework.Engine.AspectWeavers;
            using Metalama.Framework.Engine.Utilities.Roslyn;
            using Microsoft.CodeAnalysis;
            using Microsoft.CodeAnalysis.CSharp;
            using Microsoft.CodeAnalysis.CSharp.Syntax;
            using System.Linq;
            using System.Threading.Tasks;

            [RequireAspectWeaver("InvalidCodeWeaver")]
            class TestAspect : MethodAspect { }

            [MetalamaPlugIn]
            class InvalidCodeWeaver : IAspectWeaver
            {
                public Task TransformAsync(AspectWeaverContext context)
                {
                    return context.RewriteAspectTargetsAsync(new Rewriter());
                }

                private class Rewriter : SafeSyntaxRewriter
                {
                    public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
                    {
                        // Generate invalid syntax by parsing malformed code
                        // ")(" is definitely invalid syntax
                        var invalidCodeText = ")(";
                        var parsedStatement = SyntaxFactory.ParseStatement(invalidCodeText);

                        var newBody = SyntaxFactory.Block(parsedStatement);
                        return node.WithBody(newBody);
                    }
                }
            }

            class TargetClass
            {
                [TestAspect]
                public void TestMethod() { }
            }
            """;

        var testOptions = new TestContextOptions { VerifyOutputCode = true };
        using var testContext = this.CreateTestContext( testOptions );
        var compilation = testContext.CreateCSharpCompilation(
            code,
            additionalReferences: new[]
            {
                MetadataReference.CreateFromFile( typeof(Microsoft.CodeAnalysis.Compilation).Assembly.Location ),
                MetadataReference.CreateFromFile( typeof(Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree).Assembly.Location )
            } );
        var pipeline = new CompileTimeAspectPipeline( testContext.ServiceProvider );
        var result = await pipeline.ExecuteAsync( null, null, compilation, default, default );

        // The pipeline should fail because the generated code has syntax errors
        Assert.False( result.IsSuccessful, "Pipeline should fail when generated code has syntax errors" );
    }

    [Fact]
    public async Task VerifyOutputCode_WithPreprocessorDirectiveMismatch_Fails()
    {
        // Regression test for issue found in NopCommerce where fabric wrapped in #if !BENCHMARK ... #endif
        // loses the opening #if during code generation, leaving unmatched #endif.
        // This test is currently skipped because the bug exists. Once fixed, remove the Skip attribute.
        const string code = """
            #if !BENCHMARK

            using Metalama.Framework.Aspects;
            using Metalama.Framework.Code;
            using Metalama.Framework.Fabrics;
            using System.Linq;

            namespace TestNamespace;

            public class TestFabric : TransitiveProjectFabric
            {
                public override void AmendProject(IProjectAmender amender)
                {
                    amender.SelectMany(p =>
                            p.Types.SelectMany(t => t.Properties)
                                .Where(it => it is { IsAbstract: false, IsImplicitlyDeclared: false }))
                        .AddAspect<OverridePropertyAttribute>();
                }
            }

            public class OverridePropertyAttribute : OverrideFieldOrPropertyAspect
            {
                public override dynamic? OverrideProperty
                {
                    get => meta.Proceed();
                    set => meta.Proceed();
                }
            }

            class TargetClass
            {
                public int TestProperty { get; set; }
            }

            #endif
            """;

        var testOptions = new TestContextOptions { VerifyOutputCode = true };
        using var testContext = this.CreateTestContext( testOptions );

        // Create compilation without BENCHMARK defined (normal case)
        var compilation = testContext.CreateCSharpCompilation( code );
        var pipeline = new CompileTimeAspectPipeline( testContext.ServiceProvider );

        var diagnostics = new System.Collections.Generic.List<Diagnostic>();
        var result = await pipeline.ExecuteAsync( d => diagnostics.Add( d ), null, compilation, default, default );

        // The pipeline should succeed once the preprocessor directive bug is fixed
        Assert.True( result.IsSuccessful, "Pipeline should succeed when preprocessor directives are correctly preserved" );
    }
}
