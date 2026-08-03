// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Pipeline;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Threading;
using Metalama.Framework.Tests.UnitTestHelpers.TestClasses;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Pipeline.MemoryLeaks;

/// <summary>
/// Tests that <see cref="DesignTimeAspectPipelineFactory"/> releases the compilations of the callers that waited for
/// a pipeline that was never created.
/// </summary>
/// <remarks>
/// <para>
/// When a caller asks for the pipeline of a project that has not been registered yet,
/// <c>GetPipelineAndWaitAsync</c> enqueues a <see cref="TaskCompletionSource{TResult}"/> in a queue of listeners and
/// awaits it. The listeners of that queue are completed by iterating over it, but they are never dequeued, so the
/// queue only grows. A listener that is never completed keeps the continuation of its awaiter, and that continuation
/// is the suspended state machine of <c>GetPipelineAndWaitAsync</c>, which holds the <see cref="Compilation"/> that
/// the caller passed.
/// </para>
/// <para>
/// The cancellation token is registered only for the duration of the statement that enqueues the listener, not for
/// the duration of the wait, therefore cancelling the token does not release the caller. In the analysis process this
/// situation arises whenever a project is queried before its pipeline exists and the pipeline is then never created,
/// for example because the project is unloaded, is renamed, or fails to be classified as a Metalama project.
/// </para>
/// </remarks>
public sealed class PipelineFactoryMemoryLeakTests : DesignTimeTestBase
{
    public PipelineFactoryMemoryLeakTests( ITestOutputHelper logger ) : base( logger ) { }

    /// <summary>
    /// Creates the real pipeline factory, rather than the test subclass, because the test subclass overrides the
    /// method under test.
    /// </summary>
    private static DesignTimeAspectPipelineFactory CreateFactory( TestContext testContext )
    {
        GlobalServiceProvider serviceProvider = testContext.ServiceProvider;

        if ( serviceProvider.GetService<AnalysisProcessEventHub>() == null )
        {
            serviceProvider = serviceProvider.Underlying.WithService( new AnalysisProcessEventHub( serviceProvider ) );
        }

        return new DesignTimeAspectPipelineFactory( serviceProvider );
    }

    /// <summary>
    /// Starts a wait for a pipeline that does not exist, cancels it, and returns only a weak reference to the
    /// compilation that was passed to the factory.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The wait is awaited before returning, which is what makes the assertion of the caller deterministic: the
    /// continuation of the wait is what releases the objects it captured, and asserting before that continuation has
    /// run would observe a retention that is merely in flight.
    /// </para>
    /// <para>
    /// The wait is awaited under the cancellation token of the test context, so that a regression in which the wait
    /// never ends fails the test rather than hanging it. Because that guard would itself raise an operation-cancelled
    /// exception, the caller also verifies that the wait really did complete.
    /// </para>
    /// </remarks>
    [MethodImpl( MethodImplOptions.NoInlining )]
    private static async Task<WeakReference> StartAndCancelWaitAsync(
        TestContext testContext,
        DesignTimeAspectPipelineFactory factory,
        string assemblyName )
    {
        var compilation = testContext.CreateCSharpCompilation(
            new Dictionary<string, string> { ["Code.cs"] = "public class C { }" },
            assemblyName: assemblyName );

        using var cancellationTokenSource = new CancellationTokenSource();

        // No pipeline is ever created for this project, so only the cancellation token can end this wait.
        var waitTask = factory.GetPipelineAndWaitAsync( compilation, cancellationTokenSource.Token ).AsTask();

        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>( () => waitTask.WithCancellation( testContext.CancellationToken ) );

        Assert.True(
            waitTask.IsCompleted,
            "The wait for the pipeline did not end when its cancellation token was signalled. The exception above came "
            + "from the timeout of the test context, not from the wait itself." );

        return new WeakReference( compilation );
    }

    /// <summary>
    /// Verifies that cancelling a wait for a pipeline releases the compilation that the caller supplied.
    /// </summary>
    [Fact]
    public async Task CancelledWaitForPipeline_ReleasesTheCompilation()
    {
        using var testContext = this.CreateTestContext();
        using var factory = CreateFactory( testContext );

        var compilation = await StartAndCancelWaitAsync( testContext, factory, nameof(this.CancelledWaitForPipeline_ReleasesTheCompilation) );

        MemoryLeakAssert.Collected( compilation, "The compilation of a cancelled wait for a pipeline", ("pipelineFactory", factory) );
    }

    /// <summary>
    /// Verifies that a sequence of cancelled waits does not accumulate compilations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="CancelledWaitForPipeline_ReleasesTheCompilation"/> states the defect on a single wait. This test
    /// states its consequence: the collection of listeners belongs to the factory, which lives as long as the
    /// analysis process, so anything it retains is retained until the process exits.
    /// </para>
    /// <para>
    /// One survivor is tolerated, and the assertion is made for two different numbers of waits so that the claim
    /// being tested is the absence of accumulation rather than the absence of any survivor. The asynchronous state of
    /// the most recent iteration, including the exception that carries the cancelled task, is still in flight when
    /// the assertion runs, and it belongs to the test rather than to the code under test. The defect this test guards
    /// against would retain every iteration, not one.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData( 10 )]
    [InlineData( 30 )]
    public async Task RepeatedCancelledWaitsForPipeline_DoNotAccumulateCompilations( int waitCount )
    {
        using var testContext = this.CreateTestContext();
        using var factory = CreateFactory( testContext );

        var compilations = new WeakReference[waitCount];

        for ( var i = 0; i < waitCount; i++ )
        {
            compilations[i] = await StartAndCancelWaitAsync(
                testContext,
                factory,
                $"{nameof(this.RepeatedCancelledWaitsForPipeline_DoNotAccumulateCompilations)}{i}" );
        }

        MemoryLeakAssert.AtMostAlive(
            compilations,
            1,
            $"compilations of {waitCount} cancelled waits for a pipeline",
            ("pipelineFactory", factory) );
    }
}
