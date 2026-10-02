// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Compiler;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Pipeline;
using Metalama.Framework.Engine.Pipeline.CompileTime;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Services;
using Metalama.Framework.Tests.UnitTestHelpers.Mocks;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Pipeline;

/// <summary>
/// Tests of <see cref="PipelineExtension.ExecuteTransformingContributorsAsync"/> and of <see cref="ExtensionTransformationContext"/>.
/// </summary>
public sealed class TransformingPipelineExtensionTests : UnitTestClass
{
    private const string _diagnosticQueriesCode = """
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

                                                  internal class TheAspect : TypeAspect
                                                  {
                                                      public override void BuildAspect( IAspectBuilder<INamedType> builder )
                                                          => builder.Outbound.ReportDiagnostic( _ => Definitions.Warning );
                                                  }

                                                  [TheAspect]
                                                  internal class C { }
                                                  """;

    [Fact]
    public async Task SingleStage_HookCalledOnce()
    {
        var (recorder, _, result) = await this.ExecuteAsync( "class C { }" );

        Assert.True( result.IsSuccessful );

        var call = Assert.Single( recorder.Calls );
        Assert.Equal( 0, call.HighLevelStageIndex );
        Assert.True( call.IsSourceStage );
    }

    [Fact]
    public async Task SourceCompilationTreesAreInputTrees()
    {
        var (recorder, compilation, result) = await this.ExecuteAsync( "class C { }" );

        Assert.True( result.IsSuccessful );

        var call = Assert.Single( recorder.Calls );

        Assert.Equal(
            compilation.SyntaxTrees.Select( t => t.FilePath ).OrderBy( x => x ),
            call.SourceSyntaxTrees.Select( t => t.FilePath ).OrderBy( x => x ) );

        Assert.All( call.SourceSyntaxTrees, t => Assert.Contains( t, compilation.SyntaxTrees ) );
    }

    /// <summary>
    /// Verifies that the contributors added by the aspects of the stage are distinguished from the contributors replayed from the fabrics.
    /// </summary>
    [Fact]
    public async Task ContributorsAddedInStage_ExcludesReplays()
    {
        var (recorder, _, result) = await this.ExecuteAsync( _diagnosticQueriesCode );

        Assert.True( result.IsSuccessful );

        var call = Assert.Single( recorder.Calls );
        Assert.Equal( 2, call.ContributorCount );
        Assert.Equal( 1, call.ContributorsAddedInStageCount );
    }

    [Fact]
    public async Task SourceCompilationWithFinalAspectsSeesAspects()
    {
        var (recorder, _, result) = await this.ExecuteAsync( _diagnosticQueriesCode );

        Assert.True( result.IsSuccessful );

        var call = Assert.Single( recorder.Calls );
        Assert.Equal( 1, call.AspectInstanceCountOnC );
    }

    [Fact]
    public async Task ContextDiagnosticsReachResult()
    {
        var diagnostics = new List<Diagnostic>();

        var (_, _, result) = await this.ExecuteAsync( "class C { }", new HookRecorder { ReportDiagnostic = true }, diagnostics );

        Assert.True( result.IsSuccessful );
        Assert.Contains( diagnostics, d => d.Id == HookRecorder.DiagnosticId );
    }

    /// <summary>
    /// Verifies that an exception of an extension is not swallowed by the stage. It is handled by the pipeline like an exception of
    /// <see cref="PipelineExtension.ExecutePipelineContributorsAsync"/>.
    /// </summary>
    [Fact]
    public async Task ExtensionThrows_ExceptionPropagates()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>( () => this.ExecuteAsync( "class C { }", new HookRecorder { Throw = true } ) );

        Assert.Equal( "The test extension failed.", exception.Message );
    }

    [Fact]
    public void NotCalledAtDesignTime()
    {
        var recorder = new HookRecorder();
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
        var compilation = testContext.CreateCSharpCompilation( "class C { }" );

        Assert.True( factory.TryExecute( testContext.ProjectOptions, compilation, default, out _ ) );
        Assert.Empty( recorder.Calls );
    }

    private async Task<(HookRecorder Recorder, Compilation Compilation, FallibleResult<CompileTimeAspectPipelineResult> Result)> ExecuteAsync(
        string code,
        HookRecorder? recorder = null,
        List<Diagnostic>? diagnostics = null )
    {
        recorder ??= new HookRecorder();
        diagnostics ??= new List<Diagnostic>();

        var additionalServices = new AdditionalServiceCollection();
        additionalServices.AddProjectService( recorder );

        using var testContext = this.CreateTestContext(
            this.CreateDefaultTestContextOptions() with { ExtensionTypes = ImmutableArray.Create( typeof(RecordingExtension) ) },
            additionalServices );

        var pipeline = new CompileTimeAspectPipeline( testContext.ServiceProvider );
        var compilation = testContext.CreateCSharpCompilation( code );

        var result = await pipeline.ExecuteAsync( diagnostics.Add, null, compilation, ImmutableArray<ManagedResource>.Empty );

        return (recorder, compilation, result);
    }

    /// <summary>
    /// Records the calls of <see cref="RecordingExtension"/> and controls its behavior.
    /// </summary>
    private sealed class HookRecorder : IProjectService
    {
        public const string DiagnosticId = "TEST_HOOK";

        public ConcurrentQueue<HookCall> Calls { get; } = new();

        public bool ReportDiagnostic { get; init; }

        public bool Throw { get; init; }
    }

    /// <summary>
    /// The data observed by one call of the hook.
    /// </summary>
    private sealed record HookCall(
        int HighLevelStageIndex,
        bool IsSourceStage,
        ImmutableArray<SyntaxTree> SourceSyntaxTrees,
        int ContributorCount,
        int ContributorsAddedInStageCount,
        int AspectInstanceCountOnC );

    /// <summary>
    /// A transforming extension that records each call in the <see cref="HookRecorder"/> of the project.
    /// </summary>
    private sealed class RecordingExtension : PipelineExtension
    {
        public override Task ExecuteTransformingContributorsAsync( ExtensionTransformationContext context, CancellationToken cancellationToken )
        {
            var recorder = context.ServiceProvider.GetService<HookRecorder>();

            if ( recorder == null )
            {
                return Task.CompletedTask;
            }

            if ( recorder.Throw )
            {
                throw new InvalidOperationException( "The test extension failed." );
            }

            var typeC = context.SourceCompilationWithFinalAspects.Types.OfName( "C" ).SingleOrDefault();

            recorder.Calls.Enqueue(
                new HookCall(
                    context.HighLevelStageIndex,
                    context.IsSourceStage,
                    context.SourceCompilation.PartialCompilation.SyntaxTreeCollection.ToImmutableArray(),
                    context.Contributors.Count,
                    context.ContributorsAddedInStage.Count,
                    typeC?.Enhancements().GetAspectInstances().Count() ?? 0 ) );

            if ( recorder.ReportDiagnostic )
            {
                context.Diagnostics.Report(
                    Diagnostic.Create(
                        new DiagnosticDescriptor( HookRecorder.DiagnosticId, "Test", "Reported by the hook.", "Test", DiagnosticSeverity.Warning, true ),
                        Location.None ) );
            }

            return Task.CompletedTask;
        }
    }
}
