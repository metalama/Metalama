// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Compiler;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Pipeline.CompileTime;
using Metalama.Framework.Engine.ReferenceGraph;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Services;
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

namespace Metalama.Framework.Tests.UnitTests.ReferenceIndex;

/// <summary>
/// Tests of the <see cref="SourceReferenceIndexStage"/> that the compile-time pipeline starts for each high-level stage and passes to the
/// extensions.
/// </summary>
public sealed class SourceReferenceIndexPipelineTests : UnitTestClass
{
    private const string _code = """
                                 class A { public static int F() => 0; }
                                 class B { void M() => A.F(); }
                                 """;

    /// <summary>
    /// A weaver aspect that is ordered between two aspects, so that the pipeline has two high-level stages.
    /// </summary>
    private const string _twoStagesCode = """
                                          using System.Threading.Tasks;
                                          using Metalama.Framework.Aspects;
                                          using Metalama.Framework.Engine;
                                          using Metalama.Framework.Engine.AspectWeavers;

                                          [assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(Aspect1), typeof(WeaverAspect), typeof(Aspect2) )]

                                          internal class Aspect1 : TypeAspect { }

                                          internal class Aspect2 : TypeAspect { }

                                          [RequireAspectWeaver( "AspectWeaver" )]
                                          internal class WeaverAspect : TypeAspect { }

                                          [MetalamaPlugIn]
                                          internal class AspectWeaver : IAspectWeaver
                                          {
                                              public Task TransformAsync( AspectWeaverContext context ) => Task.CompletedTask;
                                          }

                                          [Aspect1] [WeaverAspect] [Aspect2] class C { }
                                          """;

    [Fact]
    public async Task StageIsPassedThroughContexts()
    {
        var recorder = new IndexRecorder();

        using var testContext = this.CreateRecordingTestContext( recorder );

        await ExecuteAsync( testContext, _code, "Assembly" );

        var call = Assert.Single( recorder.Calls );
        Assert.True( call.Stage.HasRequirements );
        Assert.Equal( ["B.M()"], call.ReferencingNames );
    }

    [Fact]
    public async Task Context_GivesHighLevelStageIndex()
    {
        var recorder = new IndexRecorder();

        using var testContext = this.CreateRecordingTestContext( recorder );

        await ExecuteAsync( testContext, _twoStagesCode, "Assembly" );

        Assert.Equal( [0, 1], recorder.RequirementStageIndexes.OrderBy( i => i ) );
        Assert.Equal( [0, 1], recorder.Calls.SelectAsArray( c => c.HighLevelStageIndex ).OrderBy( i => i ) );
    }

    /// <summary>
    /// Verifies that the pipeline disposes the stage when the linker has completed, so that an extension that keeps the stage cannot read an
    /// index that retains the compilation.
    /// </summary>
    [Fact]
    public async Task Stage_DisposedAfterLinker()
    {
        var recorder = new IndexRecorder();

        using var testContext = this.CreateRecordingTestContext( recorder );

        await ExecuteAsync( testContext, _code, "Assembly" );

        var stage = Assert.Single( recorder.Calls ).Stage;

        await Assert.ThrowsAsync<ObjectDisposedException>( () => stage.GetIndexAsync( testContext.CancellationToken ) );
    }

    /// <summary>
    /// Verifies that two pipelines that run at the same time in the same project have independent stages. The hook of each pipeline waits until
    /// the hook of the other pipeline has started, so both stages are alive at the same time.
    /// </summary>
    [Fact]
    public async Task ConcurrentPipelines_SameConfiguration_IndependentStages()
    {
        var recorder = new IndexRecorder { Rendezvous = new Rendezvous( "Assembly1", "Assembly2" ) };

        using var testContext = this.CreateRecordingTestContext( recorder );

        await Task.WhenAll(
            ExecuteAsync( testContext, _code.Replace( "class B", "class B1" ), "Assembly1" ),
            ExecuteAsync( testContext, _code.Replace( "class B", "class B2" ), "Assembly2" ) );

        var calls = recorder.Calls.ToOrderedList( c => c.AssemblyName, StringComparer.Ordinal );

        Assert.Equal( 2, calls.Count );
        Assert.NotSame( calls[0].Stage, calls[1].Stage );
        Assert.Equal( ["B1.M()"], calls[0].ReferencingNames );
        Assert.Equal( ["B2.M()"], calls[1].ReferencingNames );
    }

    private static async Task ExecuteAsync( MetalamaTestContext testContext, string code, string assemblyName )
    {
        var pipeline = new CompileTimeAspectPipeline( testContext.ServiceProvider );

        var compilation = testContext.CreateCSharpCompilation(
            code,
            assemblyName: assemblyName,
            additionalReferences:
            [
                MetadataReference.CreateFromFile( typeof(Compilation).Assembly.Location ),
                MetadataReference.CreateFromFile( typeof(CSharpSyntaxTree).Assembly.Location )
            ] );

        var diagnostics = new List<Diagnostic>();

        var result = await pipeline.ExecuteAsync( diagnostics.Add, null, compilation, ImmutableArray<ManagedResource>.Empty, testContext.CancellationToken );

        Assert.True( result.IsSuccessful, string.Join( Environment.NewLine, diagnostics ) );
    }

    [MustDisposeResource]
    private MetalamaTestContext CreateRecordingTestContext(
        IndexRecorder recorder,
        [System.Runtime.CompilerServices.CallerMemberName] string? callerMemberName = null )
    {
        var additionalServices = new AdditionalServiceCollection();
        additionalServices.AddProjectService( recorder );

        return this.CreateTestContext(
            this.CreateDefaultTestContextOptions() with { ExtensionTypes = ImmutableArray.Create( typeof(IndexingExtension) ) },
            additionalServices,
            callerMemberName: callerMemberName );
    }

    /// <summary>
    /// The data that the hook observed in one stage of one pipeline.
    /// </summary>
    private sealed record IndexCall( string AssemblyName, int HighLevelStageIndex, SourceReferenceIndexStage Stage, IReadOnlyList<string> ReferencingNames );

    /// <summary>
    /// Records what <see cref="IndexingExtension"/> observes.
    /// </summary>
    private sealed class IndexRecorder : IProjectService
    {
        public ConcurrentQueue<int> RequirementStageIndexes { get; } = new();

        public ConcurrentQueue<IndexCall> Calls { get; } = new();

        public Rendezvous? Rendezvous { get; init; }
    }

    /// <summary>
    /// Makes the hooks of two pipelines wait for each other. A <see cref="SemaphoreSlim"/> is used because its <see cref="SemaphoreSlim.WaitAsync(CancellationToken)"/>
    /// method takes a cancellation token on every target framework.
    /// </summary>
    private sealed class Rendezvous
    {
        private readonly Dictionary<string, SemaphoreSlim> _arrivals;

        public Rendezvous( string assemblyName1, string assemblyName2 )
        {
            this._arrivals = new Dictionary<string, SemaphoreSlim> { [assemblyName1] = new( 0 ), [assemblyName2] = new( 0 ) };
        }

        public async Task ArriveAsync( string assemblyName, CancellationToken cancellationToken )
        {
            this._arrivals[assemblyName].Release();

            var other = this._arrivals.Single( p => p.Key != assemblyName ).Value;

            await other.WaitAsync( cancellationToken );
        }
    }

    /// <summary>
    /// An extension that requests the invocations of the method <c>F</c> and reads the index of the stage in its transforming hook.
    /// </summary>
    private sealed class IndexingExtension : PipelineExtension
    {
        private IndexRecorder? _recorder;

        public override bool Initialize( PipelineExtensionInitializationContext context )
        {
            this._recorder = context.ServiceProvider.GetService<IndexRecorder>();

            return true;
        }

        public override SourceIndexRequirements GetSourceIndexRequirements( SourceIndexRequirementsContext context )
        {
            this._recorder?.RequirementStageIndexes.Enqueue( context.HighLevelStageIndex );

            return new SourceIndexRequirements( [new ReferenceIndexerRequirements( ReferenceKinds.Invocation, false, DeclarationKind.Method, "F" )] );
        }

        public override async Task ExecuteTransformingContributorsAsync( ExtensionTransformationContext context, CancellationToken cancellationToken )
        {
            if ( this._recorder == null )
            {
                return;
            }

            var assemblyName = context.SourceCompilation.RoslynCompilation.AssemblyName!;

            if ( this._recorder.Rendezvous != null )
            {
                await this._recorder.Rendezvous.ArriveAsync( assemblyName, cancellationToken );
            }

            var index = await context.SourceReferenceIndex.GetIndexAsync( cancellationToken );

            var referencingNames = index.ReferencedSymbols.SelectMany( s => s.References )
                .Select( r => r.ReferencingSymbol.ToTestName() )
                .Distinct()
                .ToOrderedList( x => x, StringComparer.Ordinal );

            this._recorder.Calls.Enqueue( new IndexCall( assemblyName, context.HighLevelStageIndex, context.SourceReferenceIndex, referencingNames ) );
        }
    }
}
