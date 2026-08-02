// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Pipeline;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Tests.UnitTestHelpers.Mocks;
using Metalama.Framework.Tests.UnitTestHelpers.TestClasses;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Pipeline;

/// <summary>
/// In-process reproduction of https://github.com/metalama/Metalama/issues/1748: the design-time pipeline aborts with
/// <see cref="InvalidCastException"/> when <c>DesignTimeAspectPipelineResult.SplitResultsByTree</c> casts a reference
/// to <c>IFullRef</c> and gets a <c>DeclarationIdRef&lt;INamedType&gt;</c> instead.
/// </summary>
/// <remarks>
/// <para>
/// The id-based reference comes from a transitive aspect instance that reached the consumer through the
/// <em>deserialized</em> transitive manifest. The producer of such an instance here is the pulled constructor
/// parameter: introducing a parameter with <c>PullStrategy.IntroduceParameterAndPull</c> exports a transitive aspect
/// instance targeting the declaring type of the base constructor, so that derived types in other projects also get
/// the parameter.
/// </para>
/// <para>
/// Deserialization is only taken when the producer's and the consumer's compile-time projections differ, that is,
/// when <c>TransitivePipelineContributorSource.CanReuseLiveManifest</c> is <c>false</c>. This test therefore mirrors
/// the solution shape of <see cref="CrossTfmInheritedOptionsTests"/> (issue #1710): a multi-targeted <c>Shared</c>
/// library whose per-TFM compile-time copies are both loaded, a <c>Library</c> project on the netstandard2.0 copy
/// declaring the base type, and an <c>App</c> project on the net472 copy deriving from it. The companion test
/// <see cref="PulledConstructorParameterThroughLiveManifest"/> is the same solution over the live-manifest channel,
/// which is unaffected and shows the pipeline succeeding.
/// </para>
/// </remarks>
public sealed class SplitResultsByTreeTests : DesignTimePipelineTestsBase
{
    public SplitResultsByTreeTests( ITestOutputHelper testOutput ) : base( testOutput ) { }

    /// <summary>
    /// The shared, multi-targeted library holding the aspect. Compiled once per TFM below; the differing
    /// <c>[assembly: TargetFramework]</c> is what forks it into two distinct compile-time projections
    /// (<c>ComputeSourceHash</c> folds in the target framework). The compile-time code itself is identical across the
    /// two TFMs: the copies are distinct purely by TFM identity.
    /// </summary>
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

    /// <summary>
    /// The producer project: declares the base type whose constructor carries the pulling aspect.
    /// </summary>
    private const string _libraryCode = """
                                        using Shared;

                                        public partial class C
                                        {
                                            [PullAspect]
                                            public C() { }

                                            public C( string s ) : this() { }
                                        }
                                        """;

    /// <summary>
    /// The consumer project: derives from the base type, so the transitive aspect instance exported by the pull has
    /// to be picked up here.
    /// </summary>
    private const string _appCode = """
                                    public partial class D : C
                                    {
                                        D( string s ) : base( s ) { }
                                    }
                                    """;

    /// <summary>
    /// Reproduces issue #1748: <c>App</c> consumes <c>Library</c>'s transitive manifest through the deserializing
    /// channel, so the pulled-parameter transitive aspect instance carries an id-based <c>DeclarationIdRef</c>, which
    /// <c>SplitResultsByTree</c> casts to <c>IFullRef</c>. Before the fix the design-time execution aborts.
    /// </summary>
    [Fact]
    public void PulledConstructorParameterThroughDeserializedManifest()
    {
        using var testContext = this.CreateTestContext();
        using var libraryContext = this.CreateTestContext();
        using var appContext = this.CreateTestContext();

        // Two compile-time copies of the same shared library, one per consumer TFM. This is what makes App decline
        // the live manifest and deserialize Library's one instead.
        var sharedNetStandard = testContext.CreateCSharpCompilation(
            GetSharedCode( ".NETStandard,Version=v2.0" ),
            assemblyName: "Shared" );

        var sharedNetFramework = testContext.CreateCSharpCompilation(
            GetSharedCode( ".NETFramework,Version=v4.7.2" ),
            assemblyName: "Shared" );

        var library = testContext.CreateCSharpCompilation(
            _libraryCode,
            assemblyName: "Library",
            additionalReferences: new[] { sharedNetStandard.ToMetadataReference() } );

        using var pipelineFactory = new TestDesignTimeAspectPipelineFactory( testContext );

        // Library's pipeline has to run first so its transitive manifest is available to App's pipeline.
        Assert.True( pipelineFactory.TryExecute( libraryContext.ProjectOptions, library, default, out var libraryResult ) );

        var libraryWithDesignTimeCode = this.AddDesignTimeGeneratedCode( library, libraryResult );

        var app = testContext.CreateCSharpCompilation(
            _appCode,
            assemblyName: "App",
            additionalReferences: new[] { sharedNetFramework.ToMetadataReference(), libraryWithDesignTimeCode.ToMetadataReference() } );

        this.AssertParameterIsPulled( pipelineFactory, appContext, app );
    }

    /// <summary>
    /// The control case: <c>Library</c> and <c>App</c> reference the <em>same</em> <c>Shared</c> compile-time copy, so
    /// <c>App</c> reuses the live transitive manifest and the aspect instance keeps its full reference. This path is
    /// unaffected by issue #1748 and must keep working.
    /// </summary>
    [Fact]
    public void PulledConstructorParameterThroughLiveManifest()
    {
        using var testContext = this.CreateTestContext();
        using var libraryContext = this.CreateTestContext();
        using var appContext = this.CreateTestContext();

        var shared = testContext.CreateCSharpCompilation(
            GetSharedCode( ".NETStandard,Version=v2.0" ),
            assemblyName: "Shared" );

        var library = testContext.CreateCSharpCompilation(
            _libraryCode,
            assemblyName: "Library",
            additionalReferences: new[] { shared.ToMetadataReference() } );

        using var pipelineFactory = new TestDesignTimeAspectPipelineFactory( testContext );

        Assert.True( pipelineFactory.TryExecute( libraryContext.ProjectOptions, library, default, out var libraryResult ) );

        var libraryWithDesignTimeCode = this.AddDesignTimeGeneratedCode( library, libraryResult );

        // App references the SAME Shared compilation as Library, so both compile-time closures share one copy.
        var app = testContext.CreateCSharpCompilation(
            _appCode,
            assemblyName: "App",
            additionalReferences: new[] { shared.ToMetadataReference(), libraryWithDesignTimeCode.ToMetadataReference() } );

        this.AssertParameterIsPulled( pipelineFactory, appContext, app );
    }

    /// <summary>
    /// Returns <paramref name="compilation"/> augmented with the code that the design-time pipeline generated for it,
    /// as reported by <paramref name="results"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is what a consumer project sees in the editor. The design-time pipeline cannot change the signature of an
    /// existing constructor, so it exposes a pulled parameter as an additional overload in a generated partial class,
    /// and that generated document is part of the producer's compilation as far as any referencing project is
    /// concerned. Referencing the bare compilation instead would model no real scenario: the pulled parameter would be
    /// invisible to the consumer on both channels, and the consumer could not apply the transitive aspect however its
    /// references were resolved.
    /// </para>
    /// <para>
    /// The generated documents are filed under <see cref="SourceGeneratorHelper.GeneratedFilePathSegment"/>, which is
    /// how Roslyn names the output of the Metalama source generator and how
    /// <see cref="SourceGeneratorHelper.IsGeneratedFile"/> recognizes it. The producer's own pipeline therefore ignores
    /// them, exactly as a source generator never sees its own output. Without that path, the producer's pipeline reads
    /// the generated overloads back as source and the recursive pull does not terminate.
    /// </para>
    /// </remarks>
    private Compilation AddDesignTimeGeneratedCode( Compilation compilation, DesignTimeAspectPipelineResultAndState results )
    {
        this.TestOutput.WriteLine( $"--- {compilation.AssemblyName}'s design-time results ---" );
        this.TestOutput.WriteLine( DumpResults( results ) );

        var generatedTrees = results.Result.SyntaxTreeResults.Values
            .SelectMany( r => r.Introductions )
            .Select(
                i => i.GeneratedSyntaxTree.WithFilePath(
                    $"{SourceGeneratorHelper.GeneratedFilePathSegment}/{i.Name}.cs" ) )
            .ToArray();

        Assert.NotEmpty( generatedTrees );

        return compilation.AddSyntaxTrees( generatedTrees );
    }

    /// <summary>
    /// Runs the consumer's pipeline, asserts that it completed (reporting neither an <see cref="InvalidCastException"/>
    /// nor any other failure), and asserts that the transitive aspect actually applied, that is, that the pulled
    /// parameter <c>p1</c> was introduced into the derived type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The cast failure surfaces differently depending on the entry point (bare on the source-generator path, wrapped
    /// in an <see cref="AggregateException"/> on the analyzer path, and possibly caught and turned into a diagnostic),
    /// so all three channels are inspected. That part is the regression guard for issue #1748.
    /// </para>
    /// <para>
    /// The assertion on the generated code is the regression guard for issue #1752: on the deserializing channel the
    /// aspect instance is created but its target does not resolve, so nothing is introduced and no diagnostic is
    /// reported. Completing successfully is therefore not sufficient evidence that the aspect ran.
    /// </para>
    /// </remarks>
    private void AssertParameterIsPulled( TestDesignTimeAspectPipelineFactory pipelineFactory, TestContext appContext, Compilation app )
    {
        Exception? thrown = null;
        var success = false;
        ImmutableArray<Diagnostic> diagnostics = default;
        DesignTimeAspectPipelineResultAndState? result = null;

        try
        {
            success = pipelineFactory.TryExecute( appContext.ProjectOptions, app, default, out result, out diagnostics );
        }
        catch ( Exception e )
        {
            thrown = e;
        }

        this.TestOutput.WriteLine( $"success={success}, thrown={thrown?.GetType().Name}" );

        if ( thrown != null )
        {
            this.TestOutput.WriteLine( thrown.ToString() );
        }

        if ( !diagnostics.IsDefault )
        {
            foreach ( var diagnostic in diagnostics )
            {
                this.TestOutput.WriteLine( diagnostic.ToString() );
            }
        }

        var castErrorObserved =
            (thrown != null && Flatten( thrown ).Any( e => e is InvalidCastException ))
            || (!diagnostics.IsDefault && diagnostics.Any( d => d.ToString().Contains( "cannot be cast", StringComparison.Ordinal ) ));

        Assert.False( castErrorObserved, "The design-time pipeline threw InvalidCastException (issue #1748)." );

        Assert.True( thrown == null, $"App's pipeline threw: {thrown}" );
        Assert.True( success, "App's pipeline did not succeed." );

        var generatedCode = DumpResults( result! );
        this.TestOutput.WriteLine( "--- App's design-time results ---" );
        this.TestOutput.WriteLine( generatedCode );

        // Whitespace is removed so that the assertion states the signature and the forwarded argument without
        // depending on the formatting of the generated code.
        var normalizedGeneratedCode = new string( generatedCode.Where( c => !char.IsWhiteSpace( c ) ).ToArray() );

        Assert.Contains(
            "D(global::System.Strings,global::System.Int32p1=20):this(s)",
            normalizedGeneratedCode,
            StringComparison.Ordinal );
    }

    /// <summary>
    /// Enumerates an exception together with all its inner exceptions, expanding <see cref="AggregateException"/>.
    /// </summary>
    private static IEnumerable<Exception> Flatten( Exception e )
    {
        for ( var current = e; current != null; current = current.InnerException )
        {
            yield return current;

            if ( current is AggregateException aggregate )
            {
                foreach ( var inner in aggregate.InnerExceptions.SelectMany( Flatten ) )
                {
                    yield return inner;
                }
            }
        }
    }
}
