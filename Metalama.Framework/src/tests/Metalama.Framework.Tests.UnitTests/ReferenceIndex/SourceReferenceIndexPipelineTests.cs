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
/// Tests of the <see cref="SourceReferenceIndex"/> that the compile-time pipeline creates once per pipeline execution and passes to the
/// extensions.
/// </summary>
public sealed class SourceReferenceIndexPipelineTests : UnitTestClass
{
    /// <summary>
    /// The code of the tests: the method <c>A.F</c> and an invocation of it in <c>B.M</c>.
    /// </summary>
    private const string _code = """
                                 class A { public static int F() => 0; }
                                 class B { void M() => A.F(); }
                                 """;

    /// <summary>
    /// Verifies that the context of the transforming hook passes the index of the source references, which has the requirements that the extension
    /// returned and contains the reference from <c>B.M</c>.
    /// </summary>
    [Fact]
    public async Task IndexIsPassedThroughContext()
    {
        var recorder = new IndexRecorder();

        using var testContext = this.CreateRecordingTestContext( recorder );

        await ExecuteAsync( testContext, _code, "Assembly" );

        var call = Assert.Single( recorder.Calls );
        Assert.True( call.SourceIndex.HasRequirements );
        Assert.Equal( ["B.M()"], call.ReferencingNames );
    }

    /// <summary>
    /// Verifies that the pipeline disposes the index when the linker has completed, so that an extension that keeps the index cannot read an
    /// index that retains the compilation.
    /// </summary>
    [Fact]
    public async Task Index_DisposedAfterLinker()
    {
        var recorder = new IndexRecorder();

        using var testContext = this.CreateRecordingTestContext( recorder );

        await ExecuteAsync( testContext, _code, "Assembly" );

        var sourceIndex = Assert.Single( recorder.Calls ).SourceIndex;

        await Assert.ThrowsAsync<ObjectDisposedException>( () => sourceIndex.GetIndexAsync( testContext.CancellationToken ) );
    }

    /// <summary>
    /// Verifies that two pipelines that run at the same time in the same project have independent indexes. The hook of each pipeline waits until
    /// the hook of the other pipeline has started, so both indexes are alive at the same time.
    /// </summary>
    [Fact]
    public async Task ConcurrentPipelines_SameConfiguration_IndependentIndexes()
    {
        var recorder = new IndexRecorder { Rendezvous = new Rendezvous( "Assembly1", "Assembly2" ) };

        using var testContext = this.CreateRecordingTestContext( recorder );

        await Task.WhenAll(
            ExecuteAsync( testContext, _code.Replace( "class B", "class B1" ), "Assembly1" ),
            ExecuteAsync( testContext, _code.Replace( "class B", "class B2" ), "Assembly2" ) );

        var calls = recorder.Calls.ToOrderedList( c => c.AssemblyName, StringComparer.Ordinal );

        Assert.Equal( 2, calls.Count );
        Assert.NotSame( calls[0].SourceIndex, calls[1].SourceIndex );
        Assert.Equal( ["B1.M()"], calls[0].ReferencingNames );
        Assert.Equal( ["B2.M()"], calls[1].ReferencingNames );
    }

    /// <summary>
    /// Runs the compile-time pipeline on the given code, with the given assembly name, and asserts that it succeeds.
    /// </summary>
    private static async Task ExecuteAsync( MetalamaTestContext testContext, string code, string assemblyName )
    {
        var pipeline = new CompileTimeAspectPipeline( testContext.ServiceProvider );

        var compilation = testContext.CreateCSharpCompilation( code, assemblyName: assemblyName );

        var diagnostics = new List<Diagnostic>();

        var result = await pipeline.ExecuteAsync( diagnostics.Add, null, compilation, ImmutableArray<ManagedResource>.Empty, testContext.CancellationToken );

        Assert.True( result.IsSuccessful, string.Join( Environment.NewLine, diagnostics ) );
    }

    /// <summary>
    /// Creates a test context in which <see cref="IndexingExtension"/> is loaded and records its observations in the given recorder.
    /// </summary>
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
    /// The data that the hook observed in one pipeline execution.
    /// </summary>
    private sealed record IndexCall( string AssemblyName, SourceReferenceIndex SourceIndex, IReadOnlyList<string> ReferencingNames );

    /// <summary>
    /// Records what <see cref="IndexingExtension"/> observes.
    /// </summary>
    private sealed class IndexRecorder : IProjectService
    {
        /// <summary>
        /// Gets the observations of the transforming hook.
        /// </summary>
        public ConcurrentQueue<IndexCall> Calls { get; } = new();

        /// <summary>
        /// Gets the object that makes the hooks of two pipelines wait for each other, or <c>null</c> when the hooks do not wait.
        /// </summary>
        public Rendezvous? Rendezvous { get; init; }
    }

    /// <summary>
    /// Makes the hooks of two pipelines wait for each other. A <see cref="SemaphoreSlim"/> is used because its <see cref="SemaphoreSlim.WaitAsync(CancellationToken)"/>
    /// method takes a cancellation token on every target framework.
    /// </summary>
    private sealed class Rendezvous
    {
        /// <summary>
        /// The semaphore of each assembly, which is released when the hook of the pipeline of this assembly arrives.
        /// </summary>
        private readonly Dictionary<string, SemaphoreSlim> _arrivals;

        /// <summary>
        /// Initializes a new instance of the <see cref="Rendezvous"/> class for the pipelines of two assemblies.
        /// </summary>
        public Rendezvous( string assemblyName1, string assemblyName2 )
        {
            this._arrivals = new Dictionary<string, SemaphoreSlim> { [assemblyName1] = new( 0 ), [assemblyName2] = new( 0 ) };
        }

        /// <summary>
        /// Signals that the hook of the pipeline of the given assembly has arrived, and waits until the hook of the other pipeline has arrived.
        /// </summary>
        public async Task ArriveAsync( string assemblyName, CancellationToken cancellationToken )
        {
            this._arrivals[assemblyName].Release();

            var other = this._arrivals.Single( p => p.Key != assemblyName ).Value;

            await other.WaitAsync( cancellationToken );
        }
    }

    /// <summary>
    /// An extension that requests the invocations of the method <c>F</c> and reads the index in its transforming hook.
    /// </summary>
    private sealed class IndexingExtension : PipelineExtension
    {
        /// <summary>
        /// The recorder of the project, or <c>null</c> when the test did not register one.
        /// </summary>
        private IndexRecorder? _recorder;

        public override bool Initialize( PipelineExtensionInitializationContext context )
        {
            this._recorder = context.ServiceProvider.GetService<IndexRecorder>();

            return true;
        }

        public override SourceIndexRequirements GetSourceIndexRequirements( SourceIndexRequirementsContext context )
        {
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

            this._recorder.Calls.Enqueue( new IndexCall( assemblyName, context.SourceReferenceIndex, referencingNames ) );
        }
    }
}
