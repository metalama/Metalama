// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.DesignTime.Pipeline;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Tests.UnitTestHelpers.Mocks;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
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
    /// Returns the code of the library that declares the aspects of the cross-project tests of issue #2425.
    /// </summary>
    /// <remarks>
    /// The library is compiled once per target framework. A consuming project that references the same copy as the
    /// producing project reuses the live transitive manifest of the producing project. A consuming project that
    /// references the other copy deserializes the manifest instead, and must resolve the identifiers that it contains.
    /// </remarks>
    private static string GetSharedCode( string targetFramework )
        => $$"""
             using Metalama.Framework.Advising;
             using Metalama.Framework.Aspects;
             using Metalama.Framework.Code;

             [assembly: System.Runtime.Versioning.TargetFramework("{{targetFramework}}")]

             namespace Shared
             {
                 public class PullAspect : ConstructorAspect
                 {
                     public override void BuildAspect( IAspectBuilder<IConstructor> builder )
                     {
                         builder.IntroduceParameter(
                             "p1",
                             typeof(int),
                             TypedConstant.Create( 15 ),
                             PullStrategy.IntroduceParameterAndPull( defaultValue: TypedConstant.Create( 20 ) ) );
                     }
                 }

                 [Inheritable]
                 public class InitializeAspect : TypeAspect
                 {
                     public override void BuildAspect( IAspectBuilder<INamedType> builder )
                     {
                         builder.AddInitializer( nameof(Initialize), InitializerKind.AfterLastInstanceConstructor );
                     }

                     [Template]
                     public void Initialize() { }
                 }
             }
             """;

    /// <summary>
    /// The producer of <see cref="CrossProjectIntegration_NonPartialBaseType"/>: a type that is not partial, whose
    /// parameterless constructor receives a pulled parameter.
    /// </summary>
    private const string _nonPartialProducerCode = """
                                                   using Shared;

                                                   public class C
                                                   {
                                                       [PullAspect]
                                                       public C() { }
                                                   }
                                                   """;

    /// <summary>
    /// The producer of <see cref="CrossProjectIntegration_NonPartialBaseTypeWithInheritedInitializer"/>: an abstract type
    /// that is not partial, whose implicit constructor receives the parameter of an initializer.
    /// </summary>
    private const string _nonPartialProducerWithInitializerCode = """
                                                                  using Shared;

                                                                  [InitializeAspect]
                                                                  public abstract class C
                                                                  {
                                                                  }
                                                                  """;

    /// <summary>
    /// The consumer of the cross-project tests of issue #2425: a type with an implicit constructor and a type with an
    /// explicit constructor, both derived from the type of the producer.
    /// </summary>
    private const string _nonPartialConsumerCode = """
                                                   public class D : C
                                                   {
                                                   }

                                                   public class E : C
                                                   {
                                                       public E() { }
                                                   }
                                                   """;

    /// <summary>
    /// Verifies that the design-time pipeline of a consuming project reports no error when it derives
    /// from a type whose parameterless constructor received a pulled parameter, and when that type is not partial.
    /// </summary>
    /// <remarks>
    /// The design-time pipeline cannot add a constructor overload to a type that is not partial, so the consuming
    /// project sees the parameterless constructor only. Regression test for issue #2425.
    /// </remarks>
    [Theory]
    [InlineData( true )]
    [InlineData( false )]
    public void CrossProjectIntegration_NonPartialBaseType( bool useSameSharedCopy )
        => this.AssertConsumerPipelineReportsNoError( _nonPartialProducerCode, useSameSharedCopy );

    /// <summary>
    /// Verifies that the design-time pipeline of a consuming project reports no error when it derives
    /// from an abstract type that is not partial, whose implicit constructor received the parameter of an
    /// <see cref="InitializerKind.AfterLastInstanceConstructor"/> initializer of an inheritable aspect.
    /// </summary>
    /// <remarks>
    /// This is the situation of <c>RoleBasedSecurable</c> in the authorization sample, where the exception of issue
    /// #2425 was observed.
    /// </remarks>
    [Theory]
    [InlineData( true )]
    [InlineData( false )]
    public void CrossProjectIntegration_NonPartialBaseTypeWithInheritedInitializer( bool useSameSharedCopy )
        => this.AssertConsumerPipelineReportsNoError( _nonPartialProducerWithInitializerCode, useSameSharedCopy );

    /// <summary>
    /// Executes the design-time pipeline of a producing project, then the one of a consuming project that references the
    /// producing project with its design-time generated code, and asserts that the consuming pipeline reports no error.
    /// </summary>
    /// <param name="producerCode">The code of the producing project.</param>
    /// <param name="useSameSharedCopy">
    /// When <c>true</c>, the consuming project references the same copy of the library of aspects as the producing
    /// project. When <c>false</c>, it references the copy of another target framework.
    /// </param>
    /// <remarks>
    /// <para>
    /// The transitive aspect targets the base type, which is declared in the producing project. When the aspect throws,
    /// the exception is reported as a diagnostic on the base type, so the location of the diagnostic is a document of
    /// the producing project. The design-time pipeline files a diagnostic under the document of the consuming project
    /// that has the same path, and drops the diagnostic when there is no such document.
    /// </para>
    /// <para>
    /// The consuming project is therefore given an empty document whose path is the path of the document of the
    /// producing project. Without this document, the test would pass whether the aspect throws or not.
    /// </para>
    /// </remarks>
    private void AssertConsumerPipelineReportsNoError( string producerCode, bool useSameSharedCopy )
    {
        const string producerDocumentPath = "Producer.cs";

        using var testContext = this.CreateTestContext();
        using var producerContext = this.CreateTestContext();
        using var consumerContext = this.CreateTestContext();

        var sharedForProducer = testContext.CreateCSharpCompilation( GetSharedCode( ".NETStandard,Version=v2.0" ), assemblyName: "Shared" );

        var sharedForConsumer = useSameSharedCopy
            ? sharedForProducer
            : testContext.CreateCSharpCompilation( GetSharedCode( ".NETFramework,Version=v4.7.2" ), assemblyName: "Shared" );

        var producer = testContext.CreateCSharpCompilation(
            new Dictionary<string, string> { [producerDocumentPath] = producerCode },
            assemblyName: "Producer",
            additionalReferences: [sharedForProducer.ToMetadataReference()] );

        using var pipelineFactory = new TestDesignTimeAspectPipelineFactory( testContext );

        Assert.True( pipelineFactory.TryExecute( producerContext.ProjectOptions, producer, default, out var producerResult ) );
        AssertNoError( producerResult );

        var producerWithDesignTimeCode = producer.AddSyntaxTrees( GetDesignTimeGeneratedTrees( producerResult ) );

        var consumer = testContext.CreateCSharpCompilation(
            new Dictionary<string, string> { ["Consumer.cs"] = _nonPartialConsumerCode, [producerDocumentPath] = "" },
            assemblyName: "Consumer",
            additionalReferences: [sharedForConsumer.ToMetadataReference(), producerWithDesignTimeCode.ToMetadataReference()] );

        Assert.True( pipelineFactory.TryExecute( consumerContext.ProjectOptions, consumer, default, out var consumerResult ) );
        AssertNoError( consumerResult );

        static void AssertNoError( DesignTimeAspectPipelineResultAndState result )
        {
            var errors = result.GetAllDiagnostics().Where( d => d.Severity == DiagnosticSeverity.Error ).ToArray();

            Assert.True( errors.Length == 0, string.Join( "\n", errors.Select( d => d.ToString() ) ) );
        }
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
        var generatedTrees = GetDesignTimeGeneratedTrees( results );

        Assert.NotEmpty( generatedTrees );

        return compilation.AddSyntaxTrees( generatedTrees );
    }

    /// <summary>
    /// Returns the syntax trees that the design-time pipeline generated, filed under the path by which the Metalama
    /// source generator's output is recognized.
    /// </summary>
    private static SyntaxTree[] GetDesignTimeGeneratedTrees( DesignTimeAspectPipelineResultAndState results )
        => results.Result.SyntaxTreeResults.Values
            .SelectMany( r => r.Introductions )
            .Select( i => i.GeneratedSyntaxTree.WithFilePath( $"{SourceGeneratorHelper.GeneratedFilePathSegment}/{i.Name}.cs" ) )
            .ToArray();
}
