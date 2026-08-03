// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks - acceptable in test code
#pragma warning disable VSTHRD103 // Cancel synchronously blocks - CancelAsync not available on all target frameworks

using Metalama.Framework.Tests.UnitTests.DesignTime.Pipeline.MemoryLeaks;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

// ReSharper disable AccessToDisposedClosure

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Rpc;

/// <summary>
/// Tests that the wait a server-side RPC service performs before serving a request observes its cancellation token.
/// </summary>
/// <remarks>
/// <para>
/// Every method of every server-side service begins by awaiting <c>RpcService.WaitUntilInitializedAsync</c>, and the
/// underlying <see cref="TaskCompletionSource{TResult}"/> is completed only when a client attaches to the endpoint. A
/// caller that reaches the wait while no client is attached therefore waits until one is, which in the configuration
/// where no client ever attaches means forever.
/// </para>
/// <para>
/// Waiting is correct. Waiting without observing the cancellation token is not, because a suspended asynchronous
/// method keeps its own frame alive, and with it every local and every argument it captured. On this surface those
/// arguments are Roslyn objects: the source generator awaits the wait while holding the compilation whose generated
/// sources it is publishing, and the user-process services await it while holding a compilation or a symbol. The
/// caller passes a cancellation token in each case, and cancelling it releases nothing.
/// </para>
/// <para>
/// The client side of the same wait, <c>RpcClient.WaitUntilInitializedAsync</c>, composes the token with
/// <c>WithCancellation</c> and is already covered by
/// <c>RpcClientTests.WaitUntilInitializedAsync_Cancelled_ThrowsOperationCancelledException</c>. This suite is its
/// server-side counterpart.
/// </para>
/// <para>
/// Extensions reach this surface: an extension may register its own RPC services during initialization, as
/// <c>DesignTimeExtensionRpcTests</c> demonstrates, and those services inherit the same wait. The defect is tracked
/// by issue #1799.
/// </para>
/// </remarks>
public sealed partial class RpcServiceCancellationTests : RpcUnitTestClass
{
    public RpcServiceCancellationTests( ITestOutputHelper logger ) : base( logger ) { }

    /// <summary>
    /// Verifies that the wait ends when the cancellation token given to it is cancelled.
    /// </summary>
    [Fact]
    public async Task WaitUntilInitializedAsync_Cancelled_ThrowsOperationCancelledException()
    {
        using var testContext = this.CreateShortTimeoutRpcTestContext();

        var pipeName = $"{nameof(RpcServiceCancellationTests)}_{Guid.NewGuid()}";

        using var serverEndpoint = new TestServerEndpoint( testContext.ServiceProvider, pipeName );
        serverEndpoint.Start();

        using var cts = new CancellationTokenSource();

        var waitTask = serverEndpoint.Service.WaitAsync( cts.Token );

        // No client attaches to the endpoint, so the service is never initialized and the wait cannot have completed.
        Assert.False( waitTask.IsCompleted );

        cts.Cancel();

        Assert.True(
            await EndedAsync( waitTask, testContext ),
            "The wait did not end after its cancellation token was cancelled." );

        await Assert.ThrowsAnyAsync<OperationCanceledException>( () => waitTask );
    }

    /// <summary>
    /// Verifies that cancelling the wait releases what the suspended caller was holding.
    /// </summary>
    /// <remarks>
    /// This is the consequence that matters for the memory of the analysis process, and it is stated separately from
    /// the test above because the two could in principle diverge: an implementation could complete the returned task
    /// on cancellation while leaving the original continuation attached to the
    /// <see cref="TaskCompletionSource{TResult}"/>, which would satisfy the first test and still retain the frame.
    /// </remarks>
    [Fact]
    public async Task WaitUntilInitializedAsync_Cancelled_ReleasesTheCallerFrame()
    {
        using var testContext = this.CreateShortTimeoutRpcTestContext();

        var pipeName = $"{nameof(RpcServiceCancellationTests)}_{Guid.NewGuid()}";

        using var serverEndpoint = new TestServerEndpoint( testContext.ServiceProvider, pipeName );
        serverEndpoint.Start();

        using var cts = new CancellationTokenSource();

        var (waitTask, payload) = StartWaitingWithPayload( serverEndpoint, cts.Token );

        Assert.False( waitTask.IsCompleted );

        cts.Cancel();

        // The outcome of the wait is not asserted here: it is the subject of the test above. This test observes only
        // what the suspended frame holds once its token has been cancelled, so it waits for the task to end and
        // proceeds either way.
        _ = await EndedAsync( waitTask, testContext );

        MemoryLeakAssert.Collected(
            payload,
            "The object captured by a caller suspended on the wait for initialization",
            ("serverEndpoint", serverEndpoint) );
    }

    /// <summary>
    /// Creates a test context whose timeout is short, because these tests deliberately provoke a wait that does not
    /// end, and the timeout is what bounds it.
    /// </summary>
    /// <remarks>
    /// The default timeout of a test context is four minutes, which is the right order of magnitude for a test that
    /// is expected to complete and is far too long for one whose failure mode is precisely that nothing happens.
    /// </remarks>
    private RpcTestContext CreateShortTimeoutRpcTestContext()
        => this.CreateRpcTestContext( this.CreateDefaultTestContextOptions() with { Timeout = TimeSpan.FromSeconds( 10 ) } );

    /// <summary>
    /// Waits for a task to end, bounded by the timeout of the test context, and reports whether it did.
    /// </summary>
    /// <remarks>
    /// A plain <c>await</c> would hang for as long as the defect is present, which would make a failure indefinite
    /// rather than reported. The bound is the cancellation token of the test context rather than a delay of a chosen
    /// duration, so that these tests contain no timing assumption of their own.
    /// </remarks>
    private static async Task<bool> EndedAsync( Task task, RpcTestContext testContext )
    {
        var timeout = Task.Delay( Timeout.Infinite, testContext.CancellationToken );

        return await Task.WhenAny( task, timeout ) == task;
    }

    /// <summary>
    /// Starts a caller that suspends on the wait while holding an object, and returns that object weakly.
    /// </summary>
    /// <remarks>
    /// The object is created and captured inside a method that is not inlinable, so that no frame of the calling test
    /// holds it once this method has returned. That is the same precaution as the one taken throughout the memory
    /// suite, and it is what makes the assertion above about the suspended frame and about nothing else.
    /// </remarks>
    [MethodImpl( MethodImplOptions.NoInlining )]
    private static (Task WaitTask, WeakReference Payload) StartWaitingWithPayload( TestServerEndpoint endpoint, CancellationToken cancellationToken )
    {
        var payload = new object();

        async Task WaitAsync()
        {
            await endpoint.Service.WaitAsync( cancellationToken );

            // Keeps the object captured by the state machine for the whole of the suspension, which is the point.
            GC.KeepAlive( payload );
        }

        return (WaitAsync(), new WeakReference( payload ));
    }
}
