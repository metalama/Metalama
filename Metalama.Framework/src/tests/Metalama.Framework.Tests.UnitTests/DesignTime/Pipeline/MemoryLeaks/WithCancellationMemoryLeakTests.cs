// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks - the foreign task is what these tests are about
#pragma warning disable VSTHRD103 // Cancel synchronously blocks - CancelAsync not available on all target frameworks

using System;
using System.Collections;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Pipeline.MemoryLeaks;

/// <summary>
/// Tests that waiting on a long-lived task with a cancellation token leaves nothing behind on that task when the wait
/// is cancelled, for both implementations of <c>WithCancellation</c> that the design-time code uses.
/// </summary>
/// <remarks>
/// <para>
/// Awaiting a task attaches a continuation to it, and a task releases its continuations only when it completes. A task
/// that stays incomplete for a long time would therefore accumulate one entry per wait, unless something removes them.
/// Such tasks exist on the design-time surface and are waited on with tokens that are cancelled constantly: the
/// initialization of an RPC service is completed only when a client attaches to its endpoint, and no client attaches
/// at all when the Visual Studio extension is absent.
/// </para>
/// <para>
/// Both implementations wait by way of <see cref="Task.WhenAny(Task[])"/>, which does remove its continuation from the
/// task that did not win. These tests exist to hold that property, which is a guarantee of the runtime and of a
/// third-party library rather than of this codebase, and which nothing else here would notice losing. The count is
/// asserted against a constant far below the number of waits, so that growth proportional to the number of
/// cancellations is what fails, rather than the exact figure, which is a few units of asynchronous bookkeeping still
/// in flight.
/// </para>
/// <para>
/// This class deliberately does not derive from <c>UnitTestClass</c>, unlike the rest of the suite. What it measures
/// belongs to the runtime and to Microsoft.VisualStudio.Threading, not to Metalama, so it needs neither a test context
/// nor a service provider, and constructing one would suggest that the compilation or the pipeline takes part in the
/// result. It writes through the logger it is given for the same reason.
/// </para>
/// </remarks>
public sealed class WithCancellationMemoryLeakTests( ITestOutputHelper logger )
{
    private readonly ITestOutputHelper _logger = logger;

    /// <summary>
    /// Counts the continuations attached to a task.
    /// </summary>
    /// <remarks>
    /// This reads a private field of <see cref="Task"/>, which is the only way to observe the property under test:
    /// what accumulates is invisible to every public API and holds nothing that a weak reference could track. The
    /// field holds <c>null</c> when there is no continuation, the continuation itself when there is exactly one, and a
    /// list beyond that. A future runtime may rename it, which is why the absence of the field fails the test with an
    /// explicit message rather than silently reporting zero.
    /// </remarks>
    private static int CountContinuations( Task task )
    {
        var field = typeof(Task).GetField( "m_continuationObject", BindingFlags.Instance | BindingFlags.NonPublic );

        Assert.True(
            field != null,
            "Task.m_continuationObject was not found. The runtime has changed and this test needs to be updated; it "
            + "must not be assumed to pass." );

        if ( task.IsCompleted )
        {
            // A completed task stores a sentinel in this field rather than continuations, because it has already run
            // and released them. Reading the field would count the sentinel as one continuation.
            return 0;
        }

        return field!.GetValue( task ) switch
        {
            null => 0,
            ICollection collection => collection.Count,
            _ => 1
        };
    }

    /// <summary>
    /// Verifies that the number of continuations left on a task that never completes does not grow with the number of
    /// cancelled waits.
    /// </summary>
    /// <remarks>
    /// The cases span two orders of magnitude, which is what distinguishes a constant overhead from growth. A count
    /// that tracks the number of waits is the defect; a count that stays constant is the property.
    /// </remarks>
    [Theory]
    [InlineData( 10 )]
    [InlineData( 100 )]
    [InlineData( 1000 )]
    public async Task CancelledWaits_DoNotAccumulateOnTheAwaitedTask( int waitCount )
    {
        // Never completed, standing in for the initialization of a service no client ever attaches to.
        var neverCompleted = new TaskCompletionSource<bool>();

        for ( var i = 0; i < waitCount; i++ )
        {
            using var cancellationTokenSource = new CancellationTokenSource();

            var waitTask = Metalama.Framework.Engine.Utilities.Threading.TaskExtensions.WithCancellation(
                neverCompleted.Task,
                cancellationTokenSource.Token );

            cancellationTokenSource.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>( () => waitTask );
        }

        var continuationCount = CountContinuations( neverCompleted.Task );

        this._logger.WriteLine( $"After {waitCount} cancelled waits, the task holds {continuationCount} continuation(s)." );

        AssertDoesNotGrow( continuationCount, waitCount );
    }

    /// <summary>
    /// Repeats <see cref="CancelledWaits_DoNotAccumulateOnTheAwaitedTask"/> for the implementation of
    /// <c>WithCancellation</c> that comes from Microsoft.VisualStudio.Threading.
    /// </summary>
    /// <remarks>
    /// Two of the three waits fixed by issue #1799 resolve to that implementation rather than to the one of this
    /// codebase, because Metalama.Framework.DesignTime.Rpc does not reference the engine. The property has to hold for
    /// both, and neither is ours to guarantee.
    /// </remarks>
    [Theory]
    [InlineData( 10 )]
    [InlineData( 1000 )]
    public async Task CancelledWaits_DoNotAccumulateOnTheAwaitedTask_VisualStudioThreading( int waitCount )
    {
        var neverCompleted = new TaskCompletionSource<bool>();

        for ( var i = 0; i < waitCount; i++ )
        {
            using var cancellationTokenSource = new CancellationTokenSource();

            var waitTask = Microsoft.VisualStudio.Threading.ThreadingTools.WithCancellation(
                neverCompleted.Task,
                cancellationTokenSource.Token );

            cancellationTokenSource.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>( () => waitTask );
        }

        var continuationCount = CountContinuations( neverCompleted.Task );

        this._logger.WriteLine(
            $"After {waitCount} cancelled waits through Microsoft.VisualStudio.Threading, the task holds {continuationCount} continuation(s)." );

        AssertDoesNotGrow( continuationCount, waitCount );
    }

    /// <summary>
    /// Asserts that a continuation count is a constant rather than a function of the number of waits.
    /// </summary>
    /// <remarks>
    /// The bound is generous on purpose. What it has to separate is a constant from growth proportional to
    /// <paramref name="waitCount"/>, and the largest case makes that separation two orders of magnitude wide, so a
    /// tight bound would buy nothing and would make the test sensitive to how much asynchronous bookkeeping happens to
    /// be in flight.
    /// </remarks>
    private static void AssertDoesNotGrow( int continuationCount, int waitCount )
        => Assert.True(
            continuationCount <= 16,
            $"After {waitCount} cancelled waits the task holds {continuationCount} continuations, which is not a "
            + "constant. Each cancelled wait leaves one behind, and the task never completes, therefore nothing will "
            + "ever release them." );

    /// <summary>
    /// The control for <see cref="CancelledWaits_DoNotAccumulateOnTheAwaitedTask"/>: a wait that ends because the
    /// awaited task completed leaves nothing behind either.
    /// </summary>
    /// <remarks>
    /// Without this case, a failure above could not be attributed to cancellation, since a task releases its
    /// continuations when it completes and the assertion would hold for that reason alone.
    /// </remarks>
    [Fact]
    public async Task CompletedWaits_LeaveNothingBehind()
    {
        var source = new TaskCompletionSource<bool>();

        using var cancellationTokenSource = new CancellationTokenSource();

        var waitTask = Metalama.Framework.Engine.Utilities.Threading.TaskExtensions.WithCancellation(
            source.Task,
            cancellationTokenSource.Token );

        source.SetResult( true );

        await waitTask;

        Assert.Equal( 0, CountContinuations( source.Task ) );
    }
}
