// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Compiler;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Introspection;
using Metalama.Framework.Engine.Pipeline;
using Metalama.Framework.Engine.Pipeline.CompileTime;
using Metalama.Framework.Engine.Pipeline.DesignTime;
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
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Pipeline;

/// <summary>
/// Tests of <see cref="PipelineExtension.ExecuteTransformingContributorsAsync"/> and of <see cref="ExtensionTransformationContext"/>.
/// </summary>
public sealed class TransformingPipelineExtensionTests : UnitTestClass
{
    /// <summary>
    /// The code of the tests that count the contributors: a project fabric and an aspect applied to the type <c>C</c>, each of which adds a
    /// diagnostic query.
    /// </summary>
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

    /// <summary>
    /// Verifies that, in a pipeline with a single high-level stage, the hook is called once, with the stage index zero, and that this stage is the
    /// source stage.
    /// </summary>
    [Fact]
    public async Task SingleStage_HookCalledOnce()
    {
        var (recorder, _, result) = await this.ExecuteAsync( "class C { }" );

        Assert.True( result.IsSuccessful );

        var call = Assert.Single( recorder.Calls );
        Assert.Equal( 0, call.HighLevelStageIndex );
        Assert.True( call.IsSourceStage );
    }

    /// <summary>
    /// Verifies that the syntax trees of <see cref="ExtensionTransformationContext.SourceCompilation"/> are the syntax trees of the input compilation.
    /// </summary>
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

    /// <summary>
    /// Verifies that <see cref="ExtensionTransformationContext.SourceCompilationWithFinalAspects"/> exposes the aspect instance applied to the type
    /// <c>C</c>.
    /// </summary>
    [Fact]
    public async Task SourceCompilationWithFinalAspectsSeesAspects()
    {
        var (recorder, _, result) = await this.ExecuteAsync( _diagnosticQueriesCode );

        Assert.True( result.IsSuccessful );

        var call = Assert.Single( recorder.Calls );
        Assert.Equal( 1, call.AspectInstanceCountOnC );
    }

    /// <summary>
    /// Verifies that a diagnostic reported through <see cref="ExtensionTransformationContext.Diagnostics"/> is reported by the pipeline.
    /// </summary>
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

    /// <summary>
    /// Verifies that the design-time pipeline does not call the hook.
    /// </summary>
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

        Assert.True( factory.TryExecute( testContext.ProjectOptions, compilation, TestContext.Current.CancellationToken, out _ ) );
        Assert.Empty( recorder.Calls );
    }

    /// <summary>
    /// Verifies that the pipeline that precompiles a WPF project does not call the hook.
    /// </summary>
    [Fact]
    public async Task NotCalledInWpfPrecompile()
    {
        var recorder = new HookRecorder();

        using var testContext = this.CreateRecordingTestContext( recorder );

        var pipeline = new WpfPrecompileAspectPipeline( testContext.ServiceProvider );
        var compilation = testContext.CreateCSharpCompilation( "class C { }" );

        var result = await pipeline.ExecuteAsync( null, null, compilation, default, testContext.CancellationToken );

        Assert.True( result.IsSuccessful );
        Assert.Empty( recorder.Calls );
    }

    /// <summary>
    /// Verifies that the preview pipeline calls the hook once, in the source stage.
    /// </summary>
    [Fact]
    public async Task CalledInPreview()
    {
        var recorder = new HookRecorder();

        using var testContext = this.CreateRecordingTestContext( recorder );

        var pipeline = new TestablePreviewAspectPipeline( testContext.ServiceProvider );
        var compilation = testContext.CreateCSharpCompilation( "class C { }" );
        var diagnostics = new DiagnosticBag();

        Assert.True( pipeline.InvokeTryInitialize( diagnostics, compilation, testContext.CancellationToken, out var configuration ) );

        var result = await pipeline.ExecutePreviewAsync( diagnostics, PartialCompilation.CreateComplete( compilation ), configuration!, testContext.CancellationToken );

        Assert.True( result.IsSuccessful );

        var call = Assert.Single( recorder.Calls );
        Assert.True( call.IsSourceStage );
    }

    /// <summary>
    /// Verifies that the hook runs in the introspection pipeline, which the workspaces API and the code lens use.
    /// </summary>
    [Fact]
    public async Task CalledInIntrospection()
    {
        var recorder = new HookRecorder();

        using var testContext = this.CreateRecordingTestContext( recorder );

        var compilation = testContext.CreateCompilationModel( "class C { }" );
        using var pipeline = new IntrospectionAspectPipeline( testContext.ServiceProvider, null );

        var result = await pipeline.ExecuteAsync( compilation, testContext.CancellationToken );

        Assert.True( result.HasMetalamaSucceeded );

        var call = Assert.Single( recorder.Calls );
        Assert.True( call.IsSourceStage );
    }

    /// <summary>
    /// Verifies that a low-level weaver splits the pipeline into two high-level stages, and that only the first one sees the source compilation.
    /// In the second stage, <see cref="ExtensionTransformationContext.SourceCompilationWithFinalAspects"/> throws, which the extension asserts.
    /// </summary>
    [Fact]
    public async Task WeaverSplitsStages_LaterStageIsNotSource()
    {
        var (recorder, _, result) = await this.ExecuteAsync(
            new Dictionary<string, string> { ["code.cs"] = _weaverTargetCode + "[Aspect1] [WeaverAspect] [Aspect2] class C { }", ["weaver.cs"] = _weaverCode },
            cancellationToken: TestContext.Current.CancellationToken );

        Assert.True( result.IsSuccessful );

        Assert.Equal(
            [(0, true, true), (1, false, false)],
            recorder.Calls.OrderBy( c => c.HighLevelStageIndex ).Select( c => (c.HighLevelStageIndex, c.IsSourceStage, c.AspectInstanceCountOnC.HasValue) ) );
    }

    /// <summary>
    /// Verifies that a weaver that has no aspect instance does not make the first high-level stage that the hook sees lose the source compilation.
    /// </summary>
    [Fact]
    public async Task WeaverWithoutInstancesBeforeFirstStage_FirstStageIsSource()
    {
        var (recorder, _, result) = await this.ExecuteAsync(
            new Dictionary<string, string> { ["code.cs"] = _weaverTargetCode + "[Aspect1] [Aspect2] class C { }", ["weaver.cs"] = _weaverFirstCode },
            cancellationToken: TestContext.Current.CancellationToken );

        Assert.True( result.IsSuccessful );

        Assert.True( recorder.Calls.OrderBy( c => c.HighLevelStageIndex ).First().IsSourceStage );
    }

    /// <summary>
    /// Verifies that the hook receives the cancellation token of the pipeline. The hook cancels the source of that token itself, so the test
    /// does not depend on timing.
    /// </summary>
    [Fact]
    public async Task Cancellation_IsHonored()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var recorder = new HookRecorder { CancellationTokenSource = cancellationTokenSource };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => this.ExecuteAsync( new Dictionary<string, string> { ["code.cs"] = "class C { }" }, recorder, cancellationToken: cancellationTokenSource.Token ) );

        Assert.True( Assert.Single( recorder.Calls ).IsCancellationRequested );
    }

    /// <summary>
    /// The aspects that the weaver tests apply. They do nothing, because only the number of stages matters.
    /// </summary>
    private const string _weaverTargetCode = """
                                             using Metalama.Framework.Aspects;

                                             internal class Aspect1 : TypeAspect { }

                                             internal class Aspect2 : TypeAspect { }

                                             """;

    /// <summary>
    /// A weaver aspect that is ordered between <c>Aspect1</c> and <c>Aspect2</c>, so that the pipeline has two high-level stages.
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
    /// A weaver aspect that executes before the other aspects and that no declaration uses.
    /// </summary>
    private const string _weaverFirstCode = """
                                            using System.Threading.Tasks;
                                            using Metalama.Framework.Aspects;
                                            using Metalama.Framework.Engine;
                                            using Metalama.Framework.Engine.AspectWeavers;

                                            [assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(Aspect1), typeof(Aspect2), typeof(WeaverAspect) )]

                                            [RequireAspectWeaver( "AspectWeaver" )]
                                            internal class WeaverAspect : TypeAspect { }

                                            [MetalamaPlugIn]
                                            internal class AspectWeaver : IAspectWeaver
                                            {
                                                public Task TransformAsync( AspectWeaverContext context ) => Task.CompletedTask;
                                            }
                                            """;

    /// <summary>
    /// Runs the compile-time pipeline on code that consists of a single file, with the cancellation token of the current test.
    /// </summary>
    private Task<(HookRecorder Recorder, Compilation Compilation, FallibleResult<CompileTimeAspectPipelineResult> Result)> ExecuteAsync(
        string code,
        HookRecorder? recorder = null,
        List<Diagnostic>? diagnostics = null )
        => this.ExecuteAsync( new Dictionary<string, string> { ["code.cs"] = code }, recorder, diagnostics, TestContext.Current.CancellationToken );

    /// <summary>
    /// Runs the compile-time pipeline with <see cref="RecordingExtension"/> on the given files, and returns the recorder, the input compilation and
    /// the result. The compilation references the Roslyn assemblies, which the weaver requires.
    /// </summary>
    /// <param name="code">The files of the compilation, indexed by file path.</param>
    /// <param name="recorder">The recorder of the extension, or <c>null</c> to create one.</param>
    /// <param name="diagnostics">The list that receives the diagnostics of the pipeline, or <c>null</c> to create one.</param>
    /// <param name="cancellationToken">The cancellation token of the pipeline.</param>
    private async Task<(HookRecorder Recorder, Compilation Compilation, FallibleResult<CompileTimeAspectPipelineResult> Result)> ExecuteAsync(
        Dictionary<string, string> code,
        HookRecorder? recorder = null,
        List<Diagnostic>? diagnostics = null,
        CancellationToken cancellationToken = default )
    {
        recorder ??= new HookRecorder();
        diagnostics ??= new List<Diagnostic>();

        using var testContext = this.CreateRecordingTestContext( recorder );

        var pipeline = new CompileTimeAspectPipeline( testContext.ServiceProvider );

        var compilation = testContext.CreateCSharpCompilation(
            code,
            additionalReferences:
            [
                MetadataReference.CreateFromFile( typeof(Compilation).Assembly.Location ),
                MetadataReference.CreateFromFile( typeof(CSharpSyntaxTree).Assembly.Location )
            ] );

        var result = await pipeline.ExecuteAsync( diagnostics.Add, null, compilation, ImmutableArray<ManagedResource>.Empty, cancellationToken );

        return (recorder, compilation, result);
    }

    /// <summary>
    /// Creates a test context in which <see cref="RecordingExtension"/> is loaded and records its calls in the given recorder.
    /// </summary>
    [MustDisposeResource]
    private MetalamaTestContext CreateRecordingTestContext( HookRecorder recorder, [CallerMemberName] string? callerMemberName = null )
    {
        var additionalServices = new AdditionalServiceCollection();
        additionalServices.AddProjectService( recorder );

        return this.CreateTestContext(
            this.CreateDefaultTestContextOptions() with { ExtensionTypes = ImmutableArray.Create( typeof(RecordingExtension) ) },
            additionalServices,
            callerMemberName: callerMemberName );
    }

    /// <summary>
    /// Records the calls of <see cref="RecordingExtension"/> and controls its behavior.
    /// </summary>
    private sealed class HookRecorder : IProjectService
    {
        /// <summary>
        /// The identifier of the diagnostic that the hook reports when <see cref="ReportDiagnostic"/> is <c>true</c>.
        /// </summary>
        public const string DiagnosticId = "TEST_HOOK";

        /// <summary>
        /// Gets the calls recorded by the hook.
        /// </summary>
        public ConcurrentQueue<HookCall> Calls { get; } = new();

        /// <summary>
        /// Gets a value indicating whether the hook reports a diagnostic whose identifier is <see cref="DiagnosticId"/>.
        /// </summary>
        public bool ReportDiagnostic { get; init; }

        /// <summary>
        /// Gets a value indicating whether the hook throws an <see cref="InvalidOperationException"/>.
        /// </summary>
        public bool Throw { get; init; }

        /// <summary>
        /// Gets the source of the cancellation token of the pipeline. When it is set, the hook cancels it, records whether the token that it
        /// received is cancelled, and throws if it is.
        /// </summary>
        public CancellationTokenSource? CancellationTokenSource { get; init; }
    }

    /// <summary>
    /// A preview pipeline that exposes <see cref="AspectPipeline.TryInitialize"/>.
    /// </summary>
    private sealed class TestablePreviewAspectPipeline : PreviewAspectPipeline
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestablePreviewAspectPipeline"/> class.
        /// </summary>
        public TestablePreviewAspectPipeline( ProjectServiceProvider serviceProvider )
            : base( serviceProvider, ExecutionScenario.Preview ) { }

        /// <summary>
        /// Calls <see cref="AspectPipeline.TryInitialize"/> without a hint of the compile-time syntax trees, and returns its result.
        /// </summary>
        public bool InvokeTryInitialize(
            IDiagnosticAdder diagnosticAdder,
            Compilation compilation,
            CancellationToken cancellationToken,
            out AspectPipelineConfiguration? configuration )
            => this.TryInitialize( diagnosticAdder, compilation, null, cancellationToken, out configuration );
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
        int? AspectInstanceCountOnC,
        bool IsCancellationRequested );

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

            int? aspectInstanceCountOnC;

            if ( context.IsSourceStage )
            {
                var typeC = context.SourceCompilationWithFinalAspects.Types.OfName( "C" ).SingleOrDefault();
                aspectInstanceCountOnC = typeC?.Enhancements().GetAspectInstances().Count() ?? 0;
            }
            else
            {
                Assert.Throws<InvalidOperationException>( () => context.SourceCompilationWithFinalAspects );
                aspectInstanceCountOnC = null;
            }

#pragma warning disable VSTHRD103 // CancelAsync does not exist on .NET Framework.
            recorder.CancellationTokenSource?.Cancel();
#pragma warning restore VSTHRD103

            recorder.Calls.Enqueue(
                new HookCall(
                    context.HighLevelStageIndex,
                    context.IsSourceStage,
                    context.SourceCompilation.PartialCompilation.SyntaxTreeCollection.ToImmutableArray(),
                    context.Contributors.Count,
                    context.ContributorsAddedInStage.Count,
                    aspectInstanceCountOnC,
                    cancellationToken.IsCancellationRequested ) );

            cancellationToken.ThrowIfCancellationRequested();

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
