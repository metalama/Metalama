// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Pipeline;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Tests.UnitTestHelpers.Mocks;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Pipeline;

public sealed class PullParameterTests : UnitTestClass
{
    /// <summary>
    /// Verifies that a type deriving, in another project, from a type whose constructor received a pulled parameter
    /// receives the parameter as well.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The consumer references the producer <em>including the code that the design-time pipeline generated for it</em>,
    /// which is what a consuming project sees: a project reference resolves to the compilation of the referenced
    /// project, and the compilation of a project includes its source-generated documents. The design-time pipeline
    /// cannot change the signature of an existing constructor, so it exposes the pulled parameter as an additional
    /// overload in a generated partial class, and that overload is part of what the consumer sees.
    /// </para>
    /// <para>
    /// Referencing the bare compilation instead models no real scenario, because the pulled parameter would then be
    /// invisible to the consumer on both channels. This test did reference the bare compilation, and it was red for
    /// that reason rather than because of a defect. That was established by running the same solution through the
    /// design-time host simulator, where a plain project reference is enough for the transitive aspect to apply. The
    /// same arrangement is used by <see cref="SplitResultsByTreeTests"/> and
    /// <see cref="TransitiveAspectAcrossProjectsTests"/>, which document it as well. See issue #1797.
    /// </para>
    /// </remarks>
    [Fact]
    public void CrossProjectIntegration()
    {
        using var testContext = this.CreateTestContext();

        const string code1 = @"
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

public class Aspect1 : ConstructorAspect
{
    public override void BuildAspect( IAspectBuilder<IConstructor> builder )
    {
        builder.IntroduceParameter(
            ""p1"",
            typeof(int),
            TypedConstant.Create( 15 ),
            PullStrategy.IntroduceParameterAndPull( defaultValue: TypedConstant.Create( 20 ) ) );
    }
}

public partial class C
{
    [Aspect1]
    public C() { }

    public C( string s ) : this() { }
}

";

        const string code2 = """
                             partial class D : C
                             {
                               D( string s ) : base( s ) {}
                             }
                             """;

        using var testContext1 = this.CreateTestContext();

        var compilation1 = testContext.CreateCSharpCompilation( code1 );

        using var testContext2 = this.CreateTestContext();

        // We have to execute the pipeline on compilation1 first and explicitly because implicit running is not currently possible
        // because of missing project options.

        using var pipelineFactory = new TestDesignTimeAspectPipelineFactory( testContext );

        Assert.True( pipelineFactory.TryExecute( testContext1.ProjectOptions, compilation1, default, out var compilationResult1 ) );

        var compilation1WithDesignTimeCode = AddDesignTimeGeneratedCode( compilation1, compilationResult1 );

        var compilation2 = testContext.CreateCSharpCompilation(
            code2,
            additionalReferences: [compilation1WithDesignTimeCode.ToMetadataReference()] );

        Assert.True( pipelineFactory.TryExecute( testContext2.ProjectOptions, compilation2, default, out var compilationResult2 ) );

        Assert.Single( compilationResult2.Result.IntroducedSyntaxTrees );
    }

    /// <summary>
    /// Returns <paramref name="compilation"/> augmented with the code that the design-time pipeline generated for it,
    /// filed under the path by which the Metalama source generator's output is recognized.
    /// </summary>
    /// <remarks>
    /// The path matters: without it the producer's own pipeline reads its output back as source and the recursive pull
    /// does not terminate.
    /// </remarks>
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
