// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Pipeline;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Tests.UnitTestHelpers.Mocks;
using Metalama.Framework.Tests.UnitTestHelpers.TestClasses;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Pipeline;

/// <summary>
/// Verifies that the code the design-time pipeline generates for a constructor parameter pull actually compiles.
/// </summary>
/// <remarks>
/// <para>
/// No test asserted this before. <see cref="SplitResultsByTreeTests"/> and
/// <see cref="TransitiveAspectAcrossProjectsTests"/> do build the generated code into a compilation, but only in order
/// to take a metadata reference to it, which validates nothing, and neither asks that compilation for its diagnostics.
/// <c>TransitiveContributorMemoryLeakTests</c> drives the same aspect shape as the tests below but asserts on retention
/// and on the number of contributors, not on what was generated. The aspect tests that cover a forwarding constructor,
/// <c>Preserved_ChainedConstructor</c> and its siblings, have no design-time variant, and the one design-time test of
/// that area, <c>Preserved_DesignTime</c>, uses <c>PullStrategy.UseExpression</c> rather than
/// <c>IntroduceParameterAndPull</c>.
/// </para>
/// <para>
/// The gap matters because the design-time and the compile-time results differ by construction. A forwarding
/// constructor preserves the signature the constructor had before the aspect ran. At compile time Metalama changes the
/// signature of the original constructor, so the forwarder does not collide with it. At design time it cannot change
/// the signature of an existing constructor and adds the extended one beside it, so whether the two collide is a
/// question that only arises at design time and that only a compiled result can answer.
/// </para>
/// <para>
/// These tests were written from a design-time standalone scenario that reported <c>CS0111</c> and <c>CS0121</c> on
/// the generated code. See issue #1797.
/// </para>
/// </remarks>
public sealed class DesignTimeGeneratedCodeCompilesTests : DesignTimePipelineTestsBase
{
    public DesignTimeGeneratedCodeCompilesTests( ITestOutputHelper testOutput ) : base( testOutput ) { }

    /// <summary>
    /// The aspect, parameterized by the pull strategy, so that the strategy is the only thing that differs between the
    /// case under test and its control.
    /// </summary>
    private static string GetAspectCode( string pullStrategy )
        => $$"""
             using Metalama.Framework.Advising;
             using Metalama.Framework.Aspects;
             using Metalama.Framework.Code;
             using Metalama.Framework.Code.SyntaxBuilders;
             using System;

             public class PullAspect : TypeAspect
             {
                 public override void BuildAspect( IAspectBuilder<INamedType> builder )
                 {
                     foreach ( var constructor in builder.Target.Constructors )
                     {
                         builder.With( constructor )
                             .IntroduceParameter( "creationTime", typeof(DateTime), pullStrategy: {{pullStrategy}} );
                     }
                 }
             }

             [PullAspect]
             public partial class BaseClass
             {
                 public BaseClass( int id ) { }
             }
             """;

    /// <summary>
    /// The pull strategy that emits a forwarding constructor preserving the pre-aspect signature.
    /// </summary>
    private const string _forwarderStrategy =
        """PullStrategy.IntroduceParameterAndPull( forwarderExpression: ExpressionFactory.Parse( "global::System.DateTime.Now" ) )""";

    /// <summary>
    /// The pull strategy that gives the introduced parameter a default value, so that no forwarding constructor is
    /// needed. This is what the pre-existing design-time tests use.
    /// </summary>
    private const string _defaultValueStrategy =
        """PullStrategy.IntroduceParameterAndPull( defaultValue: TypedConstant.Default( TypeFactory.GetType( typeof(DateTime) ) ) )""";

    /// <summary>
    /// Verifies that the generated code compiles when the pull strategy gives the introduced parameter a default value.
    /// </summary>
    /// <remarks>
    /// This is the control. It uses the strategy that <see cref="SplitResultsByTreeTests"/> and
    /// <see cref="TransitiveAspectAcrossProjectsTests"/> use, so a failure here would mean the assertion itself is
    /// wrong rather than that the forwarding constructor is at fault.
    /// </remarks>
    [Fact]
    public void PullWithDefaultValue_GeneratedCodeCompiles()
    {
        using var testContext = this.CreateTestContext();
        this.AssertGeneratedCodeCompiles( testContext, GetAspectCode( _defaultValueStrategy ) );
    }

    /// <summary>
    /// Verifies that the generated code compiles when the pull strategy emits a forwarding constructor.
    /// </summary>
    /// <remarks>
    /// The forwarder preserves the signature the constructor had before the aspect ran, which at design time is the
    /// signature the constructor still has, because the design-time pipeline cannot change it.
    /// </remarks>
    [Fact]
    public void PullWithForwarderExpression_GeneratedCodeCompiles()
    {
        using var testContext = this.CreateTestContext();
        this.AssertGeneratedCodeCompiles( testContext, GetAspectCode( _forwarderStrategy ) );
    }

    /// <summary>
    /// Verifies that the code generated for a project deriving from a type of a referenced project compiles.
    /// </summary>
    /// <remarks>
    /// The consumer references the producer as a consuming project sees it, that is, including the code that the
    /// design-time pipeline generated for it, because a project reference resolves to the compilation of the
    /// referenced project and the compilation of a project includes its source-generated documents. The transitive
    /// aspect exported by the pull has to introduce the parameter into the derived constructor, and the result has to
    /// compile.
    /// </remarks>
    [Fact]
    public void PullAcrossProjects_GeneratedCodeOfTheConsumerCompiles()
    {
        using var testContext = this.CreateTestContext();
        using var libraryContext = this.CreateTestContext();
        using var consumerContext = this.CreateTestContext();

        var library = testContext.CreateCSharpCompilation( GetAspectCode( _forwarderStrategy ), assemblyName: "Library" );

        const string consumerCode = """
                                    public partial class DerivedClass : BaseClass
                                    {
                                        public DerivedClass( int id ) : base( id ) { }
                                    }
                                    """;

        using var pipelineFactory = new TestDesignTimeAspectPipelineFactory( testContext );

        // The producer's pipeline runs first, so that its transitive manifest is available to the consumer's.
        Assert.True( pipelineFactory.TryExecute( libraryContext.ProjectOptions, library, default, out var libraryResult ) );

        var consumer = testContext.CreateCSharpCompilation(
            consumerCode,
            assemblyName: "Consumer",
            additionalReferences: [AddDesignTimeGeneratedCode( library, libraryResult ).ToMetadataReference()] );

        Assert.True( pipelineFactory.TryExecute( consumerContext.ProjectOptions, consumer, default, out var consumerResult ) );

        // Asserted separately from the compilation, so that a failure distinguishes the transitive aspect not applying
        // at all from it applying and producing code that does not compile.
        Assert.NotEmpty( consumerResult.Result.SyntaxTreeResults.Values.SelectMany( r => r.Introductions ) );

        this.AssertCompiles( AddDesignTimeGeneratedCode( consumer, consumerResult ) );
    }

    /// <summary>
    /// Runs the design-time pipeline over <paramref name="code"/>, adds what it generated to the compilation, and
    /// asserts that the result compiles.
    /// </summary>
    private void AssertGeneratedCodeCompiles( TestContext testContext, string code )
    {
        using var projectContext = this.CreateTestContext();

        var compilation = testContext.CreateCSharpCompilation( code );

        using var pipelineFactory = new TestDesignTimeAspectPipelineFactory( testContext );

        Assert.True( pipelineFactory.TryExecute( projectContext.ProjectOptions, compilation, default, out var result ) );

        this.AssertCompiles( AddDesignTimeGeneratedCode( compilation, result ) );
    }

    /// <summary>
    /// Asserts that <paramref name="compilation"/> has no error, writing every one of them and the generated code that
    /// carries them, because a failure here is about code that no source file contains.
    /// </summary>
    private void AssertCompiles( Compilation compilation )
    {
        var errors = compilation.GetDiagnostics().Where( d => d.Severity == DiagnosticSeverity.Error ).ToArray();

        if ( errors.Length > 0 )
        {
            foreach ( var tree in compilation.SyntaxTrees.Where( SourceGeneratorHelper.IsGeneratedFile ) )
            {
                this.TestOutput.WriteLine( $"--- {tree.FilePath} ---" );
                this.TestOutput.WriteLine( tree.ToString() );
            }

            foreach ( var error in errors )
            {
                this.TestOutput.WriteLine( error.ToString() );
            }
        }

        Assert.Empty( errors );
    }

    /// <summary>
    /// Returns <paramref name="compilation"/> augmented with the code that the design-time pipeline generated for it,
    /// filed under the path by which the Metalama source generator's output is recognized.
    /// </summary>
    private static Compilation AddDesignTimeGeneratedCode( Compilation compilation, DesignTimeAspectPipelineResultAndState results )
    {
        var generatedTrees = results.Result.SyntaxTreeResults.Values
            .SelectMany( r => r.Introductions )
            .Select( i => i.GeneratedSyntaxTree.WithFilePath( $"{SourceGeneratorHelper.GeneratedFilePathSegment}/{i.Name}.cs" ) )
            .ToArray();

        Assert.NotEmpty( generatedTrees );

        return compilation.AddSyntaxTrees( generatedTrees );
    }
}
