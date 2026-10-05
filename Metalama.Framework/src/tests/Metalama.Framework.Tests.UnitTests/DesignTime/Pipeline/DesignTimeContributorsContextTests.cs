// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Compiler;
using Metalama.Framework.DesignTime.Pipeline;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Pipeline.CompileTime;
using Metalama.Framework.Engine.ReferenceGraph;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Framework.Services;
using Metalama.Framework.Tests.UnitTestHelpers.Mocks;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Pipeline;

/// <summary>
/// Tests of <see cref="DesignTimeContributorsContext"/>, which the design-time pipeline passes to the extensions only in the source stage, and of
/// the results of the high-level stages that the design-time result keeps (change S1, issue #2098).
/// </summary>
/// <remarks>
/// The test code has a project fabric and two aspects that add extension contributors (diagnostic queries). With a low-level weaver aspect
/// ordered between the two aspects, the pipeline has two high-level stages. The contributor of the fabric is replayed in both stages.
/// The aspect order is given in the run-time direction, so <c>Aspect2</c> executes in the first stage and <c>Aspect1</c> in the second.
/// </remarks>
public sealed class DesignTimeContributorsContextTests : UnitTestClass
{
    /// <summary>
    /// The code shared by the tests: the diagnostic definitions, the project fabric and the two aspects. The fabric and each aspect add a
    /// diagnostic query, and each aspect introduces a method.
    /// </summary>
    private const string _commonCode = """
                                       using Metalama.Framework.Aspects;
                                       using Metalama.Framework.Code;
                                       using Metalama.Framework.Diagnostics;
                                       using Metalama.Framework.Fabrics;

                                       [CompileTime]
                                       internal static class Definitions
                                       {
                                           public static readonly DiagnosticDefinition Warning = new( "MY001", Severity.Warning, "Warning." );

                                           public static readonly DiagnosticDefinition FabricWarning = new( "MY002", Severity.Warning, "Fabric warning." );
                                       }

                                       internal class Fabric : ProjectFabric
                                       {
                                           public override void AmendProject( IProjectAmender amender )
                                               => amender.SelectTypes().ReportDiagnostic( _ => Definitions.FabricWarning );
                                       }

                                       internal class Aspect1 : TypeAspect
                                       {
                                           public override void BuildAspect( IAspectBuilder<INamedType> builder )
                                               => builder.Outbound.ReportDiagnostic( _ => Definitions.Warning );

                                           [Introduce]
                                           public void IntroducedByAspect1() { }
                                       }

                                       [Inheritable]
                                       internal class Aspect2 : TypeAspect
                                       {
                                           public override void BuildAspect( IAspectBuilder<INamedType> builder )
                                               => builder.Outbound.ReportDiagnostic( _ => Definitions.Warning );

                                           [Introduce]
                                           public void IntroducedByAspect2() { }
                                       }

                                       """;

    /// <summary>
    /// The code of a pipeline that has a single high-level stage, in which both aspects are applied to the type <c>C</c>.
    /// </summary>
    private const string _singleStageCode = _commonCode + """
                                                          [Aspect1]
                                                          [Aspect2]
                                                          public class C { }
                                                          """;

    /// <summary>
    /// The code of a pipeline that has two high-level stages, because the weaver aspect is applied to the type <c>C</c> between the two aspects.
    /// </summary>
    private const string _twoStagesCode = _commonCode + """
                                                        [Aspect1]
                                                        [WeaverAspect]
                                                        [Aspect2]
                                                        public class C { }
                                                        """;

    /// <summary>
    /// The weaver aspect, which is in a separate file because declaring it is enough to split the pipeline into two high-level stages.
    /// </summary>
    private const string _weaverCode = """
                                       using System.Threading.Tasks;
                                       using Metalama.Framework.Aspects;
                                       using Metalama.Framework.Engine;
                                       using Metalama.Framework.Engine.AspectWeavers;

                                       [assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(Aspect1), typeof(WeaverAspect), typeof(Aspect2) )]

                                       [RequireAspectWeaver( "AspectWeaver" )]
                                       internal class WeaverAspect : TypeAspect { }

                                       [MetalamaPlugIn]
                                       internal class AspectWeaver : IAspectWeaver
                                       {
                                           public Task TransformAsync( AspectWeaverContext context ) => Task.CompletedTask;
                                       }
                                       """;

    /// <summary>
    /// Initializes a new instance of the <see cref="DesignTimeContributorsContextTests"/> class.
    /// </summary>
    public DesignTimeContributorsContextTests( ITestOutputHelper testOutput ) : base( testOutput ) { }

    /// <summary>
    /// Verifies that, with a single high-level stage, the extension is called once, with the contributor of the fabric and the contributors of the
    /// two aspects.
    /// </summary>
    [Fact]
    public void SingleStage_ReceivesAllContributors()
    {
        var (recorder, _) = this.Execute( _singleStageCode );

        Assert.Equal( 3, Assert.Single( recorder.Calls ) );
    }

    /// <summary>
    /// Verifies that, when a low-level weaver splits the pipeline into two high-level stages, the extension is called once, in the source stage,
    /// with the contributor of the fabric and the contributor of the aspect that executes in that stage.
    /// </summary>
    [Fact]
    public void WeaverSplitsStages_CalledOnlyInSourceStage()
    {
        var (recorder, _) = this.Execute( _twoStagesCode );

        Assert.Equal( 2, Assert.Single( recorder.Calls ) );
    }

    /// <summary>
    /// Verifies that the transitive contributors returned in the source stage are part of the design-time result, which is read from the last
    /// stage. The extension returns one transitive contributor per contributor.
    /// </summary>
    [Fact]
    public void TransitiveContributors_KeptFromSourceStage()
    {
        var (_, result) = this.Execute( _twoStagesCode );

        Assert.Equal( 2, result.Extensions.Extensions.OfType<RecordedContributor>().Count() );
    }

    /// <summary>
    /// Verifies that an inheritable aspect of the first stage is part of the design-time result, once. The aspect order is given in the run-time
    /// direction, so <c>Aspect2</c> executes in the first stage.
    /// </summary>
    [Fact]
    public void InheritableAspects_AccumulatedAcrossStages()
    {
        var (_, result) = this.Execute( _twoStagesCode );

        Assert.Single( result.GetInheritableAspects( "Aspect2" ) );
    }

    /// <summary>
    /// Verifies that the transformations of the first stage are part of the design-time result, which is read from the last stage. Each aspect
    /// introduces a method, and the aspects execute in different stages.
    /// </summary>
    [Fact]
    public void Transformations_AccumulatedAcrossStages()
    {
        var (_, result) = this.Execute( _twoStagesCode );

        var aspectClasses = result.SyntaxTreeResults.Values
            .SelectMany( r => r.Transformations )
            .Select( t => t.AspectClassFullName )
            .Distinct()
            .OrderBy( x => x, StringComparer.Ordinal );

        Assert.Equal( ["Aspect1", "Aspect2"], aspectClasses );
    }

    /// <summary>
    /// Verifies that the design-time validators returned in the source stage are part of the design-time result, which is read from the last
    /// stage. The extension returns one validator of the type <c>C</c> per contributor.
    /// </summary>
    [Fact]
    public void DesignTimeValidators_KeptFromSourceStage()
    {
        var (_, result, compilation) = this.ExecuteWithCompilation( _twoStagesCode );

        var validators = result.Extensions.GetValidatorsForSymbol( compilation.GetTypeByMetadataName( "C" ).AssertNotNull() );

        Assert.Equal( 2, validators.OfType<RecordedValidator>().Count() );
    }

    /// <summary>
    /// Verifies that the diagnostic query of the fabric, whose contributor is replayed in both stages, reports each diagnostic once.
    /// </summary>
    [Fact]
    public void DiagnosticQuery_ReplayedContributor_ReportedOnce()
    {
        var (_, result) = this.Execute( _twoStagesCode );

        var diagnostics = result.SyntaxTreeResults.Values
            .SelectMany( r => r.Diagnostics )
            .Where( d => d.Id == "MY002" )
            .Select( d => d.ToString() )
            .ToList();

        Assert.NotEmpty( diagnostics );
        Assert.Equal( diagnostics.Distinct().OrderBy( d => d, StringComparer.Ordinal ), diagnostics.OrderBy( d => d, StringComparer.Ordinal ) );
    }

    /// <summary>
    /// Verifies that the diagnostic query of the fabric, whose contributor is replayed in both stages, reports each diagnostic once at compile time.
    /// </summary>
    [Fact( Skip = "The compile-time pipeline evaluates replayed contributors in every stage (#2113)." )]
    public async Task CompileTime_DiagnosticQuery_ReplayedContributor_ReportedOnce()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCSharpCompilation(
            new Dictionary<string, string> { ["code.cs"] = _twoStagesCode, ["weaver.cs"] = _weaverCode },
            additionalReferences: [MetadataReference.CreateFromFile( typeof(Compilation).Assembly.Location ), MetadataReference.CreateFromFile( typeof(CSharpSyntaxTree).Assembly.Location )] );

        var pipeline = new CompileTimeAspectPipeline( testContext.ServiceProvider );
        var reported = new List<Diagnostic>();

        var result = await pipeline.ExecuteAsync( reported.Add, null, compilation, ImmutableArray<ManagedResource>.Empty, Xunit.TestContext.Current.CancellationToken );

        Assert.True( result.IsSuccessful, string.Join( Environment.NewLine, reported ) );

        var diagnostics = reported.Where( d => d.Id == "MY002" ).Select( d => d.ToString() ).ToList();

        Assert.NotEmpty( diagnostics );
        Assert.Equal( diagnostics.Distinct().OrderBy( d => d, StringComparer.Ordinal ), diagnostics.OrderBy( d => d, StringComparer.Ordinal ) );
    }

    /// <summary>
    /// Executes the design-time pipeline on the given code and returns the recorder and the result.
    /// </summary>
    private (StageRecorder Recorder, DesignTimeAspectPipelineResult Result) Execute( string code )
    {
        var (recorder, result, _) = this.ExecuteWithCompilation( code );

        return (recorder, result);
    }

    /// <summary>
    /// Executes the design-time pipeline on the given code, with the weaver in a separate file when the code uses it, and returns the compilation
    /// together with the result.
    /// </summary>
    private (StageRecorder Recorder, DesignTimeAspectPipelineResult Result, Compilation Compilation) ExecuteWithCompilation( string code )
    {
        var recorder = new StageRecorder();
        using var testContext = this.CreateRecordingTestContext( recorder );
        using var factory = new TestDesignTimeAspectPipelineFactory( testContext );

        var compilation = CreateCompilation(
            testContext,
            code.Contains( "[WeaverAspect]", StringComparison.Ordinal )
                ? new Dictionary<string, string> { ["code.cs"] = code, ["weaver.cs"] = _weaverCode }
                : new Dictionary<string, string> { ["code.cs"] = code } );

        Assert.True( factory.TryExecute( testContext.ProjectOptions, compilation, Xunit.TestContext.Current.CancellationToken, out var executed ) );

        return (recorder, executed.Result, compilation);
    }

    /// <summary>
    /// Creates a test context in which <see cref="RecordingExtension"/> is loaded and records its calls in the given recorder.
    /// </summary>
    private MetalamaTestContext CreateRecordingTestContext( StageRecorder recorder )
    {
        var additionalServices = new AdditionalServiceCollection();
        additionalServices.AddProjectService( recorder );

        return this.CreateTestContext(
            this.CreateDefaultTestContextOptions() with
            {
                ExtensionTypes = ImmutableArray.Create( typeof(RecordingExtension) ),
                DesignTimeExtensionTypes = ImmutableArray.Create( typeof(RecordingExtension) )
            },
            additionalServices );
    }

    /// <summary>
    /// Creates a compilation that references the Roslyn assemblies, which the weaver requires.
    /// </summary>
    private static Compilation CreateCompilation( MetalamaTestContext testContext, Dictionary<string, string> code )
        => testContext.CreateCSharpCompilation(
            code,
            additionalReferences: [MetadataReference.CreateFromFile( typeof(Compilation).Assembly.Location ), MetadataReference.CreateFromFile( typeof(CSharpSyntaxTree).Assembly.Location )] );

    private sealed class StageRecorder : IProjectService
    {
        /// <summary>
        /// Gets the number of contributors passed to each call of <see cref="PipelineExtension.ExecuteDesignTimePipelineContributorsAsync"/>, as
        /// recorded by <see cref="RecordingExtension"/>.
        /// </summary>
        public ConcurrentQueue<int> Calls { get; } = new();
    }

    private sealed class RecordingExtension : PipelineExtension
    {
        public override Task<ExtensionPipelineContributorsResult> ExecuteDesignTimePipelineContributorsAsync(
            DesignTimeContributorsContext context,
            CancellationToken cancellationToken )
        {
            var recorder = context.ServiceProvider.GetService<StageRecorder>();

            if ( recorder == null )
            {
                return Task.FromResult( ExtensionPipelineContributorsResult.Empty );
            }

            recorder.Calls.Enqueue( context.Contributors.Count );

            var transitiveContributors = ImmutableArray.CreateBuilder<ITransitivePipelineContributor>();

            var validatedType = context.FinalCompilation.RoslynCompilation.GetTypeByMetadataName( "C" );

            for ( var i = 0; i < context.Contributors.Count; i++ )
            {
                transitiveContributors.Add( new RecordedContributor() );

                if ( validatedType != null )
                {
                    transitiveContributors.Add( new RecordedValidator( SymbolDictionaryKey.CreatePersistentKey( validatedType ) ) );
                }
            }

            return Task.FromResult( new ExtensionPipelineContributorsResult( transitiveContributors.ToImmutable(), ImmutableUserDiagnosticList.Empty ) );
        }
    }

    /// <summary>
    /// A project-local transitive contributor that <see cref="RecordingExtension"/> returns.
    /// </summary>
    private sealed class RecordedContributor : ITransitivePipelineContributor, IDesignTimePipelineResultExtension
    {
        /// <summary>
        /// The project-local kind of <see cref="RecordedContributor"/>.
        /// </summary>
        private static readonly ContributorKind<RecordedContributor> _kind = new( nameof(RecordedContributor) ) { IsProjectTransitive = false };

        /// <inheritdoc />
        public ContributorKind ContributorKind => _kind;

        /// <inheritdoc />
        public DocumentKey DocumentKey => default;

        /// <inheritdoc />
        public IDesignTimePipelineResultExtension ToDesignTime() => this;

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/>, because a project-local result is never exported.
        /// </summary>
        public ITransitiveAspectsManifestExtension ToTransitiveAspectManifestExtension()
            => throw new InvalidOperationException( "A project-local result is never exported." );
    }

    /// <summary>
    /// A design-time validator of the type <c>C</c> that <see cref="RecordingExtension"/> returns.
    /// </summary>
    private sealed class RecordedValidator : ITransitivePipelineContributor, IDesignTimeValidatorExtension, ITransitiveAspectsManifestExtension
    {
        /// <summary>
        /// The design-time validator kind of <see cref="RecordedValidator"/>.
        /// </summary>
        private static readonly ContributorKind<RecordedValidator> _kind = new( nameof(RecordedValidator) ) { IsDesignTimeValidator = true };

        /// <summary>
        /// Initializes a new instance of the <see cref="RecordedValidator"/> class.
        /// </summary>
        public RecordedValidator( SymbolDictionaryKey validatedDeclaration )
        {
            this.ValidatedDeclaration = validatedDeclaration;
        }

        /// <inheritdoc />
        public ContributorKind ContributorKind => _kind;

        /// <inheritdoc />
        public DocumentKey DocumentKey => default;

        /// <inheritdoc />
        public ReferenceIndexerRequirements? ReferenceIndexerRequirements => null;

        /// <inheritdoc />
        public SymbolDictionaryKey ValidatedDeclaration { get; }

        /// <inheritdoc />
        public IDesignTimePipelineResultExtension ToDesignTime() => this;

        /// <inheritdoc />
        public ITransitiveAspectsManifestExtension ToTransitiveAspectManifestExtension() => this;
    }
}
