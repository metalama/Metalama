// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Pipeline;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Services;
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
/// Tests of <see cref="DesignTimeContributorsContext"/> and of the accumulation of the results of the high-level stages of the design-time
/// pipeline (change S1, issue #2098).
/// </summary>
/// <remarks>
/// The test code has a project fabric and two aspects that add extension contributors (diagnostic queries). With a low-level weaver aspect
/// ordered between the two aspects, the pipeline has two high-level stages. The contributor of the fabric is replayed in both stages.
/// The aspect order is given in the run-time direction, so <c>Aspect2</c> executes in the first stage and <c>Aspect1</c> in the second.
/// </remarks>
public sealed class DesignTimeContributorsContextTests : UnitTestClass
{
    private const string _commonCode = """
                                       using Metalama.Framework.Aspects;
                                       using Metalama.Framework.Code;
                                       using Metalama.Framework.Diagnostics;
                                       using Metalama.Framework.Fabrics;

                                       [CompileTime]
                                       internal static class Definitions
                                       {
                                           public static readonly DiagnosticDefinition Warning = new( "MY001", Severity.Warning, "Warning." );
                                       }

                                       internal class Fabric : ProjectFabric
                                       {
                                           public override void AmendProject( IProjectAmender amender )
                                               => amender.SelectTypes().ReportDiagnostic( _ => Definitions.Warning );
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

    private const string _singleStageCode = _commonCode + """
                                                          [Aspect1]
                                                          [Aspect2]
                                                          public class C { }
                                                          """;

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

    public DesignTimeContributorsContextTests( ITestOutputHelper testOutput ) : base( testOutput ) { }

    [Fact]
    public void SingleStage_ReceivesAllContributors_IndexZero()
    {
        var (recorder, _) = this.Execute( _singleStageCode );

        var call = Assert.Single( recorder.Calls );
        Assert.Equal( new StageCall( 0, 3, 2, 3 ), call );
    }

    /// <summary>
    /// Verifies that the stage after the weaver passes the contributor of the fabric again in <see cref="DesignTimeContributorsContext.Contributors"/>,
    /// but not in <see cref="DesignTimeContributorsContext.NewContributors"/>.
    /// </summary>
    [Fact]
    public void WeaverSplitsStages_LaterStageReceivesOnlyAddedContributors()
    {
        var (recorder, _) = this.Execute( _twoStagesCode );

        Assert.Equal( [new StageCall( 0, 2, 1, 2 ), new StageCall( 1, 2, 1, 1 )], recorder.Calls.OrderBy( c => c.HighLevelStageIndex ) );
    }

    /// <summary>
    /// Verifies that the transitive contributors returned in the first stage are part of the design-time result, which is read from the last
    /// stage. The extension returns one transitive contributor per new contributor.
    /// </summary>
    [Fact]
    public void TransitiveContributors_AccumulatedAcrossStages()
    {
        var (_, result) = this.Execute( _twoStagesCode );

        Assert.Equal( 3, result.Extensions.Extensions.OfType<RecordedContributor>().Count() );
        Assert.Equal( [0, 0, 1], result.Extensions.Extensions.OfType<RecordedContributor>().Select( c => c.HighLevelStageIndex ).OrderBy( i => i ) );
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

    private (StageRecorder Recorder, DesignTimeAspectPipelineResult Result) Execute( string code )
    {
        var recorder = new StageRecorder();
        var additionalServices = new AdditionalServiceCollection();
        additionalServices.AddProjectService( recorder );

        using var testContext = this.CreateTestContext(
            this.CreateDefaultTestContextOptions() with
            {
                ExtensionTypes = ImmutableArray.Create( typeof(RecordingExtension) ),
                DesignTimeExtensionTypes = ImmutableArray.Create( typeof(RecordingExtension) )
            },
            additionalServices );

        using var factory = new TestDesignTimeAspectPipelineFactory( testContext );

        var compilation = testContext.CreateCSharpCompilation(
            code.Contains( "[WeaverAspect]", StringComparison.Ordinal )
                ? new Dictionary<string, string> { ["code.cs"] = code, ["weaver.cs"] = _weaverCode }
                : new Dictionary<string, string> { ["code.cs"] = code },
            additionalReferences: [MetadataReference.CreateFromFile( typeof(Compilation).Assembly.Location ), MetadataReference.CreateFromFile( typeof(CSharpSyntaxTree).Assembly.Location )] );

        Assert.True( factory.TryExecute( testContext.ProjectOptions, compilation, default, out var executed ) );

        return (recorder, executed.Result);
    }

    /// <summary>
    /// The counts observed by one call of <see cref="PipelineExtension.ExecuteDesignTimePipelineContributorsAsync"/>.
    /// </summary>
    private sealed record StageCall( int HighLevelStageIndex, int ContributorCount, int ContributorsAddedInStageCount, int NewContributorCount );

    private sealed class StageRecorder : IProjectService
    {
        public ConcurrentQueue<StageCall> Calls { get; } = new();
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

            recorder.Calls.Enqueue(
                new StageCall(
                    context.HighLevelStageIndex,
                    context.Contributors.Count,
                    context.ContributorsAddedInStage.Count,
                    context.NewContributors.Count ) );

            var transitiveContributors = ImmutableArray.CreateBuilder<ITransitivePipelineContributor>();

            for ( var i = 0; i < context.NewContributors.Count; i++ )
            {
                transitiveContributors.Add( new RecordedContributor( context.HighLevelStageIndex ) );
            }

            return Task.FromResult( new ExtensionPipelineContributorsResult( transitiveContributors.ToImmutable(), ImmutableUserDiagnosticList.Empty ) );
        }
    }

    /// <summary>
    /// A project-local transitive contributor that records the stage that returned it.
    /// </summary>
    private sealed class RecordedContributor : ITransitivePipelineContributor, IDesignTimePipelineResultExtension
    {
        private static readonly ContributorKind<RecordedContributor> _kind = new( nameof(RecordedContributor) ) { IsProjectLocal = true };

        public RecordedContributor( int highLevelStageIndex )
        {
            this.HighLevelStageIndex = highLevelStageIndex;
        }

        public int HighLevelStageIndex { get; }

        public ContributorKind ContributorKind => _kind;

        public DocumentKey DocumentKey => default;

        public IDesignTimePipelineResultExtension ToDesignTime() => this;

        public ITransitiveAspectsManifestExtension ToTransitiveAspectManifestExtension()
            => throw new InvalidOperationException( "A project-local result is never exported." );
    }
}
