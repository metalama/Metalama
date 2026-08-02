// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Pipeline;
using Metalama.Framework.Engine.Collections;
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
/// Tests the view that a consuming project has of a producing project at design time, and the equivalence of the two
/// channels by which a transitive aspect reaches the consumer.
/// </summary>
/// <remarks>
/// <para>
/// These tests come from the review of pull request #1784. The reason a reference to a pulled constructor parameter did
/// not resolve at design time is general and not specific to that parameter: a consumer did not see the surface that a
/// referenced project introduces. The first two tests pin that, and the last two pin the property that the two channels
/// have to produce the same result.
/// </para>
/// <para>
/// The solution shape is the one of <see cref="SplitResultsByTreeTests"/>: a multi-targeted <c>Shared</c> library
/// holding the aspect, a <c>Library</c> project declaring the base type, and an <c>App</c> project deriving from it.
/// Whether <c>App</c> takes the live or the deserializing channel is decided by whether it references the same
/// compile-time copy of <c>Shared</c> as <c>Library</c>.
/// </para>
/// </remarks>
public sealed class TransitiveAspectAcrossProjectsTests : DesignTimePipelineTestsBase
{
    public TransitiveAspectAcrossProjectsTests( ITestOutputHelper testOutput ) : base( testOutput ) { }

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
             }
             """;

    private const string _libraryCode = """
                                        using Shared;

                                        public partial class C
                                        {
                                            [PullAspect]
                                            public C() { }

                                            public C( string s ) : this() { }
                                        }
                                        """;

    private const string _appCode = """
                                    public partial class D : C
                                    {
                                        D( string s ) : base( s ) { }
                                    }
                                    """;

    /// <summary>
    /// The producer of <see cref="BothChannelsProduceTheSameGeneratedCodeWithAnInitializer"/>: the same base type, with
    /// an <c>OnConstructed</c> initializer added beside the pulled parameter.
    /// </summary>
    private const string _libraryWithInitializerCode = """
                                                       using Metalama.Framework.Advising;
                                                       using Metalama.Framework.Aspects;
                                                       using Metalama.Framework.Code;
                                                       using Shared;

                                                       public class InitializeAspect : TypeAspect
                                                       {
                                                           public override void BuildAspect( IAspectBuilder<INamedType> builder )
                                                           {
                                                               builder.AddInitializer( nameof(Initialize), InitializerKind.AfterLastInstanceConstructor );
                                                           }

                                                           [Template]
                                                           public void Initialize() { }
                                                       }

                                                       [InitializeAspect]
                                                       public partial class C
                                                       {
                                                           [PullAspect]
                                                           public C() { }

                                                           public C( string s ) : this() { }
                                                       }
                                                       """;

    /// <summary>
    /// Verifies that the consumer's code model of the producer includes the constructor overload that the producer's
    /// aspect introduced.
    /// </summary>
    /// <remarks>
    /// <para>
    /// At compile time the consumer is compiled against the producer's transformed output, so the overload is part of
    /// the producer's surface. At design time the consumer used to see the producer before transformation, because
    /// <c>SymbolRef.Strategy.IsValidSymbol</c> rejected every symbol declared in a document produced by the Metalama
    /// source generator, whichever assembly declared it, unlike the rule immediately above it, which hides the private
    /// symbols of external assemblies only.
    /// </para>
    /// <para>
    /// Hiding is necessary for the current project, because a source generator must not read its own output back, but
    /// applying it to a referenced project made the consumer's design-time view of that project differ from what the
    /// project ships, so that every reference into a declaration a referenced project introduced was unresolvable at
    /// design time. That is the general defect of which the pulled constructor parameter of #1752 is one instance, and
    /// the hiding is now scoped to the current assembly.
    /// </para>
    /// </remarks>
    [Fact]
    public void ConsumerSeesConstructorOverloadIntroducedByProducer()
    {
        using var testContext = this.CreateTestContext();
        using var libraryContext = this.CreateTestContext();

        var shared = testContext.CreateCSharpCompilation( GetSharedCode( ".NETStandard,Version=v2.0" ), assemblyName: "Shared" );

        var library = testContext.CreateCSharpCompilation(
            _libraryCode,
            assemblyName: "Library",
            additionalReferences: [shared.ToMetadataReference()] );

        using var pipelineFactory = new TestDesignTimeAspectPipelineFactory( testContext );

        Assert.True( pipelineFactory.TryExecute( libraryContext.ProjectOptions, library, default, out var libraryResult ) );

        var libraryWithDesignTimeCode = this.AddDesignTimeGeneratedCode( library, libraryResult );

        var app = testContext.CreateCSharpCompilation(
            _appCode,
            assemblyName: "App",
            additionalReferences: [shared.ToMetadataReference(), libraryWithDesignTimeCode.ToMetadataReference()] );

        var appModel = testContext.CreateCompilationModel( app );

        var baseType = appModel.Types.OfName( "D" ).Single().BaseType;

        Assert.NotNull( baseType );

        var constructorSignatures = baseType.Constructors
            .SelectAsArray( c => string.Join( ",", c.Parameters.SelectAsArray( p => p.Type.ToString() ) ) )
            .OrderBy( s => s )
            .ToArray();

        this.TestOutput.WriteLine( $"Constructors of '{baseType.Name}' as seen by App: [{string.Join( "] [", constructorSignatures )}]" );

        Assert.Contains( constructorSignatures, s => s.Contains( "int", System.StringComparison.Ordinal ) );
    }

    /// <summary>
    /// The producer of the generalization test: an aspect that introduces an ordinary method, so that the invisibility
    /// of the introduced surface is shown not to be a particularity of constructors.
    /// </summary>
    private const string _libraryWithIntroducedMethodCode = """
                                                            using Metalama.Framework.Aspects;

                                                            public class IntroduceAspect : TypeAspect
                                                            {
                                                                [Introduce]
                                                                public int IntroducedMethod() => 42;
                                                            }

                                                            [IntroduceAspect]
                                                            public partial class E { }
                                                            """;

    private const string _appDerivingFromIntroducedCode = """
                                                          public partial class F : E { }
                                                          """;

    /// <summary>
    /// Verifies that the consumer's code model of the producer includes a method that the producer's aspect
    /// introduced.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is <see cref="ConsumerSeesConstructorOverloadIntroducedByProducer"/> generalized from a constructor overload
    /// to an ordinary member, and it uses no transitive aspect at all. It establishes that the whole surface a producer
    /// introduces is part of a consumer's design-time code model, therefore that a reference into that surface can be
    /// resolved whatever mechanism carries it.
    /// </para>
    /// </remarks>
    [Fact]
    public void ConsumerSeesMethodIntroducedByProducer()
    {
        using var testContext = this.CreateTestContext();
        using var libraryContext = this.CreateTestContext();

        var library = testContext.CreateCSharpCompilation( _libraryWithIntroducedMethodCode, assemblyName: "Library" );

        using var pipelineFactory = new TestDesignTimeAspectPipelineFactory( testContext );

        Assert.True( pipelineFactory.TryExecute( libraryContext.ProjectOptions, library, default, out var libraryResult ) );

        var libraryWithDesignTimeCode = this.AddDesignTimeGeneratedCode( library, libraryResult );

        var app = testContext.CreateCSharpCompilation(
            _appDerivingFromIntroducedCode,
            assemblyName: "App",
            additionalReferences: [libraryWithDesignTimeCode.ToMetadataReference()] );

        var appModel = testContext.CreateCompilationModel( app );

        var baseType = appModel.Types.OfName( "F" ).Single().BaseType;

        Assert.NotNull( baseType );

        var methodNames = baseType.Methods.SelectAsArray( m => m.Name ).OrderBy( n => n, System.StringComparer.Ordinal ).ToArray();

        this.TestOutput.WriteLine( $"Methods of '{baseType.Name}' as seen by App: {string.Join( ", ", methodNames )}" );

        Assert.Contains( "IntroducedMethod", methodNames, System.StringComparer.Ordinal );
    }

    /// <summary>
    /// Verifies that the code generated for the consumer is identical whether the transitive aspect reached it through
    /// the live manifest or through the deserialized one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The two channels reach the pulled parameter by different routes, the live manifest keeping the full reference
    /// and the deserialized one an identifier that has to be resolved, so their agreement is a property to assert
    /// rather than to assume. The existing tests assert a single expected signature on each channel, which does not
    /// compare the channels to each other.
    /// </para>
    /// <para>
    /// The comparison is made on the whole design-time output, with whitespace removed so that it does not depend on
    /// formatting.
    /// </para>
    /// </remarks>
    [Fact]
    public void BothChannelsProduceTheSameGeneratedCode() => this.AssertChannelsAgree( _libraryCode );

    /// <summary>
    /// Verifies the same equivalence when the producer also adds an <c>OnConstructed</c> initializer, which exports a
    /// second transitive aspect.
    /// </summary>
    /// <remarks>
    /// <c>OnConstructedMethodAdvice</c> exports <c>AddConstructorEpilogueTransitiveAspect</c>, which is documented to
    /// run after <c>PullConstructorParameterTransitiveAspect</c> in the system layer ordering, so the parameter that
    /// the pull resolved is observed by a second aspect and the two are exercised together.
    /// </remarks>
    [Fact]
    public void BothChannelsProduceTheSameGeneratedCodeWithAnInitializer() => this.AssertChannelsAgree( _libraryWithInitializerCode );

    private void AssertChannelsAgree( string libraryCode )
    {
        var throughLiveManifest = this.RunConsumerPipeline( libraryCode, useSameSharedCopy: true );
        var throughDeserializedManifest = this.RunConsumerPipeline( libraryCode, useSameSharedCopy: false );

        this.TestOutput.WriteLine( "--- live manifest ---" );
        this.TestOutput.WriteLine( throughLiveManifest );
        this.TestOutput.WriteLine( "--- deserialized manifest ---" );
        this.TestOutput.WriteLine( throughDeserializedManifest );

        Assert.Equal( Normalize( throughLiveManifest ), Normalize( throughDeserializedManifest ) );

        static string Normalize( string code ) => new( code.Where( c => !char.IsWhiteSpace( c ) ).ToArray() );
    }

    /// <summary>
    /// Runs the producer's and then the consumer's design-time pipeline and returns the consumer's results.
    /// </summary>
    /// <param name="useSameSharedCopy">
    /// When <c>true</c>, the consumer references the same compile-time copy of <c>Shared</c> as the producer, so it
    /// reuses the live transitive manifest. When <c>false</c>, it references the copy of the other target framework, so
    /// it deserializes the producer's manifest instead.
    /// </param>
    private string RunConsumerPipeline( string libraryCode, bool useSameSharedCopy )
    {
        using var testContext = this.CreateTestContext();
        using var libraryContext = this.CreateTestContext();
        using var appContext = this.CreateTestContext();

        var sharedForLibrary = testContext.CreateCSharpCompilation( GetSharedCode( ".NETStandard,Version=v2.0" ), assemblyName: "Shared" );

        var sharedForApp = useSameSharedCopy
            ? sharedForLibrary
            : testContext.CreateCSharpCompilation( GetSharedCode( ".NETFramework,Version=v4.7.2" ), assemblyName: "Shared" );

        var library = testContext.CreateCSharpCompilation(
            libraryCode,
            assemblyName: "Library",
            additionalReferences: [sharedForLibrary.ToMetadataReference()] );

        using var pipelineFactory = new TestDesignTimeAspectPipelineFactory( testContext );

        Assert.True( pipelineFactory.TryExecute( libraryContext.ProjectOptions, library, default, out var libraryResult ) );

        var libraryWithDesignTimeCode = this.AddDesignTimeGeneratedCode( library, libraryResult );

        var app = testContext.CreateCSharpCompilation(
            _appCode,
            assemblyName: "App",
            additionalReferences: [sharedForApp.ToMetadataReference(), libraryWithDesignTimeCode.ToMetadataReference()] );

        Assert.True( pipelineFactory.TryExecute( appContext.ProjectOptions, app, default, out var appResult ) );

        // Only the generated code is returned, and not the whole dump of the results, because the name of a generated
        // document is a hash that also covers the identity of the Shared copy the consumer references, which differs
        // between the two channels by construction and says nothing about the code that was generated.
        var introductions = appResult.Result.SyntaxTreeResults.Values
            .SelectMany( r => r.Introductions )
            .Select( i => i.GeneratedSyntaxTree.ToString() )
            .OrderBy( t => t, System.StringComparer.Ordinal )
            .ToArray();

        Assert.NotEmpty( introductions );

        return string.Join( "\n", introductions );
    }

    /// <summary>
    /// Returns <paramref name="compilation"/> augmented with the code that the design-time pipeline generated for it,
    /// filed under the path by which the Metalama source generator's output is recognized.
    /// </summary>
    /// <remarks>
    /// This is what a consumer project sees in the editor. The path matters: without it the producer's own pipeline
    /// reads its output back as source and the recursive pull does not terminate.
    /// </remarks>
    private Compilation AddDesignTimeGeneratedCode( Compilation compilation, DesignTimeAspectPipelineResultAndState results )
    {
        var generatedTrees = results.Result.SyntaxTreeResults.Values
            .SelectMany( r => r.Introductions )
            .Select( i => i.GeneratedSyntaxTree.WithFilePath( $"{SourceGeneratorHelper.GeneratedFilePathSegment}/{i.Name}.cs" ) )
            .ToArray();

        Assert.NotEmpty( generatedTrees );

        return compilation.AddSyntaxTrees( generatedTrees );
    }
}
