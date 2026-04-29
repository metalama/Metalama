// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Framework.DesignTime.Contracts.Notifications;
using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.DesignTime.Rpc.Notifications;
using Metalama.Framework.DesignTime.VisualStudio.Notifications;
using Metalama.Testing.UnitTesting;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Notifications;

#pragma warning disable VSTHRD200, VSTHRD003

/// <summary>
/// Unit tests for <see cref="DesignTimeNotificationService"/>: subscription, dispatch, filtering, disposal,
/// and exception isolation between observers. The end-to-end wiring from <c>ServiceHubServerEndpoint.InProcessEventReceived</c>
/// is exercised separately by integration tests; these tests construct the service through its internal logger-only constructor
/// and inject events through the internal <c>Publish</c> method, so the service's behavior is observable without standing up
/// a real RPC pipe.
/// </summary>
/// <remarks>
/// The service dispatches each event to each observer via fire-and-forget <c>Task.Run</c>, so observer callbacks complete asynchronously.
/// Tests therefore rely on <see cref="TaskCompletionSource{TResult}"/> + <see cref="WaitAsync{T}"/> with a generous timeout
/// rather than hardcoded delays. To assert that an event was *not* delivered, we use a barrier pattern: publish a known-firing
/// event after the negative case and wait for it; once the barrier completes we know the earlier publish has been fully processed.
/// </remarks>
public sealed class DesignTimeNotificationServiceTests : UnitTestClass
{
    // Generous upper bound — tests should normally complete in milliseconds. The timeout exists only to fail cleanly
    // if the dispatch path regresses (otherwise xUnit would hang the whole run).
    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds( 30 );

    public DesignTimeNotificationServiceTests( ITestOutputHelper testOutput ) : base( testOutput ) { }

    // Builds the service against the test context's xUnit-backed logger factory so that any service-side traces
    // surface in the test output rather than being silently swallowed.
    private DesignTimeNotificationService CreateService( TestContext testContext )
        => new( testContext.ServiceProvider.Global.Underlying.GetLoggerFactory().GetLogger( nameof(DesignTimeNotificationService) ) );

    private static CompilationResultChangedEventData NewCompilationResultEventData( bool isPartialCompilation = false, params string[] paths )
        => new(
            new ProjectKey( "TestProject", 0xdeadbeef, isMetalamaEnabled: true ),
            isPartialCompilation,
            paths.Length == 0 ? ImmutableArray<string>.Empty : ImmutableArray.Create( paths ) );

    /// <summary>
    /// Happy path: an observer subscribed to <c>CompilationResultChanged</c> receives a translated
    /// <see cref="ICompilationResultChangedEvent"/> when the service publishes the corresponding RPC event.
    /// Also asserts the field translation (string ProjectKey, isPartial, syntax-tree paths).
    /// </summary>
    [Fact]
    public async Task Subscribe_DispatchesMatchingEvent()
    {
        using var testContext = this.CreateTestContext();
        var service = this.CreateService( testContext );
        var tcs = new TaskCompletionSource<IDesignTimeNotificationEvent>( TaskCreationOptions.RunContinuationsAsynchronously );
        var observer = new TestObserver( evt => tcs.TrySetResult( evt ) );

        using var subscription = service.Subscribe( observer, [DesignTimeNotificationEventTypes.CompilationResultChanged] );

        service.Publish( NewCompilationResultEventData( isPartialCompilation: true, "a.cs", "b.cs" ) );

        using var cts = new CancellationTokenSource( _timeout );
        var received = await WaitAsync( tcs.Task, cts.Token );

        // The observer's parameter type is the base IDesignTimeNotificationEvent; verify the runtime type is the
        // expected derived [Guid]-marked interface and that all translated fields match the source RpcEventData.
        var evt = Assert.IsAssignableFrom<ICompilationResultChangedEvent>( received );
        Assert.Equal( DesignTimeNotificationEventTypes.CompilationResultChanged, evt.EventTypeName );
        Assert.True( evt.IsPartialCompilation );
        Assert.Equal( ["a.cs", "b.cs"], evt.SyntaxTreePaths );
    }

    /// <summary>
    /// Filtering: when an observer subscribes only to a subset of event types, events of other types must not reach it.
    /// Uses the EndpointChanged event as a barrier — receiving it confirms that the earlier (unsubscribed-to)
    /// CompilationResultChanged publish was fully processed without firing the observer.
    /// </summary>
    [Fact]
    public async Task Subscribe_FiltersByEventTypeName()
    {
        using var testContext = this.CreateTestContext();
        var service = this.CreateService( testContext );
        var tcs = new TaskCompletionSource<IDesignTimeNotificationEvent>( TaskCreationOptions.RunContinuationsAsynchronously );
        var observer = new TestObserver( evt => tcs.TrySetResult( evt ) );

        // The observer subscribes only to EndpointChanged; CompilationResultChanged events should be filtered out.
        using var subscription = service.Subscribe( observer, [DesignTimeNotificationEventTypes.EndpointChanged] );

        // First publish: a CompilationResultChanged event. If filtering is broken this would complete the TCS
        // with the wrong event type and the assertions below would fail.
        service.Publish( NewCompilationResultEventData() );

        // Barrier publish: an EndpointChanged event. Once we observe this, we know any earlier dispatch work
        // for the CompilationResultChanged event has also been processed (the service's lock serializes them).
        var projectGuid = Guid.NewGuid();
        service.Publish( new EndpointChangedEventData( projectGuid ) );

        using var cts = new CancellationTokenSource( _timeout );
        var received = await WaitAsync( tcs.Task, cts.Token );

        var evt = Assert.IsAssignableFrom<IEndpointChangedEvent>( received );
        Assert.Equal( DesignTimeNotificationEventTypes.EndpointChanged, evt.EventTypeName );
        Assert.Equal( projectGuid, evt.ProjectGuid );
    }

    /// <summary>
    /// Disposing the <see cref="IDisposable"/> returned from <see cref="IDesignTimeNotificationService.Subscribe"/>
    /// must remove the observer; subsequent events of the same type must not reach it.
    /// </summary>
    /// <remarks>
    /// Asserting "did not happen" is structurally tricky. The barrier pattern works here because
    /// <c>OnRpcEventReceived</c> takes <c>_sync</c>, snapshots the observer list, and only then fires Task.Run
    /// per observer. Since <c>Subscription.Dispose</c> takes the same lock and removes the observer from
    /// <c>_observersByEventType</c>, by the time the second Publish snapshots the list the disposed observer is no
    /// longer in it. Once the barrier observer fires, we are guaranteed the second Publish completed its critical
    /// section and the disposed observer was *not* scheduled for invocation.
    /// </remarks>
    [Fact]
    public async Task Dispose_Unsubscribes()
    {
        using var testContext = this.CreateTestContext();
        var service = this.CreateService( testContext );
        var firstTcs = new TaskCompletionSource<bool>( TaskCreationOptions.RunContinuationsAsynchronously );
        var secondTcs = new TaskCompletionSource<bool>( TaskCreationOptions.RunContinuationsAsynchronously );
        var calls = 0;

        // The first invocation completes firstTcs (proving wire-up); a hypothetical second invocation
        // (which must NOT happen after Dispose) would complete secondTcs. The final assertion checks that
        // secondTcs was never completed.
        var observer = new TestObserver(
            _ =>
            {
                if ( Interlocked.Increment( ref calls ) == 1 )
                {
                    firstTcs.TrySetResult( true );
                }
                else
                {
                    secondTcs.TrySetResult( true );
                }
            } );

        var subscription = service.Subscribe( observer, [DesignTimeNotificationEventTypes.CompilationResultChanged] );

        // Sanity check: confirm the subscription is wired up by publishing once and waiting for the first call.
        service.Publish( NewCompilationResultEventData() );
        using var cts1 = new CancellationTokenSource( _timeout );
        await WaitAsync( firstTcs.Task, cts1.Token );

        subscription.Dispose();

        // Add a barrier observer that fires on the next publish; once we observe its TCS completing, we know
        // the second publish has been fully processed by the service. At that point the disposed observer must
        // not have been scheduled (see the remarks on the test).
        var barrierTcs = new TaskCompletionSource<bool>( TaskCreationOptions.RunContinuationsAsynchronously );
        var barrier = new TestObserver( _ => barrierTcs.TrySetResult( true ) );
        using var barrierSubscription = service.Subscribe( barrier, [DesignTimeNotificationEventTypes.CompilationResultChanged] );

        service.Publish( NewCompilationResultEventData() );
        using var cts2 = new CancellationTokenSource( _timeout );
        await WaitAsync( barrierTcs.Task, cts2.Token );

        Assert.False( secondTcs.Task.IsCompleted, "Disposed subscription must not receive further events." );
        Assert.Equal( 1, calls );
    }

    /// <summary>
    /// One observer throwing from <c>OnEvent</c> must not prevent other observers from receiving the same event.
    /// This is the contract that lets a misbehaving VSX observer not stall the analyzer's event pipeline.
    /// </summary>
    [Fact]
    public async Task MultipleObservers_OneThrowing_OthersStillReceive()
    {
        using var testContext = this.CreateTestContext();
        var service = this.CreateService( testContext );
        var goodTcs = new TaskCompletionSource<bool>( TaskCreationOptions.RunContinuationsAsynchronously );

        // The bad observer throws synchronously from OnEvent. The service must catch and continue dispatching
        // to the good observer (which completes goodTcs). If the service propagated the exception out of
        // Task.Run we would still pass — but if it stopped iterating the snapshot, goodTcs would never complete
        // and the test would time out.
        var bad = new TestObserver( _ => throw new InvalidOperationException( "intentional" ) );
        var good = new TestObserver( _ => goodTcs.TrySetResult( true ) );

        using var sub1 = service.Subscribe( bad, [DesignTimeNotificationEventTypes.CompilationResultChanged] );
        using var sub2 = service.Subscribe( good, [DesignTimeNotificationEventTypes.CompilationResultChanged] );

        service.Publish( NewCompilationResultEventData() );

        using var cts = new CancellationTokenSource( _timeout );
        await WaitAsync( goodTcs.Task, cts.Token );
    }

    /// <summary>
    /// A single observer subscribed to multiple event types must receive events of each type.
    /// Verifies that an observer registered with two type names is added to both per-type lists.
    /// </summary>
    [Fact]
    public async Task SameObserver_TwoEventTypes_DispatchesBoth()
    {
        using var testContext = this.CreateTestContext();
        var service = this.CreateService( testContext );
        var seen = new List<string>();
        var sync = new object();
        var tcs = new TaskCompletionSource<bool>( TaskCreationOptions.RunContinuationsAsynchronously );

        // OnEvent is invoked from background Task.Run continuations and may interleave; we serialize writes
        // to seen with a lock and only complete the TCS once both expected event types have arrived.
        var observer = new TestObserver(
            evt =>
            {
                lock ( sync )
                {
                    seen.Add( evt.EventTypeName );

                    if ( seen.Count == 2 )
                    {
                        tcs.TrySetResult( true );
                    }
                }
            } );

        using var subscription = service.Subscribe(
            observer,
            [DesignTimeNotificationEventTypes.CompilationResultChanged, DesignTimeNotificationEventTypes.EndpointChanged] );

        service.Publish( NewCompilationResultEventData() );
        service.Publish( new EndpointChangedEventData( Guid.NewGuid() ) );

        using var cts = new CancellationTokenSource( _timeout );
        await WaitAsync( tcs.Task, cts.Token );

        // Order is not guaranteed across the two background continuations, so check membership rather than order.
        lock ( sync )
        {
            Assert.Contains( DesignTimeNotificationEventTypes.CompilationResultChanged, seen );
            Assert.Contains( DesignTimeNotificationEventTypes.EndpointChanged, seen );
        }
    }

    // Awaits a task with cancellation. We don't use Task.WaitAsync(CancellationToken) because the test project
    // also targets net48, where that overload is unavailable.
    private static async Task<T> WaitAsync<T>( Task<T> task, CancellationToken cancellationToken )
    {
        var completed = await Task.WhenAny( task, Task.Delay( Timeout.Infinite, cancellationToken ) );

        if ( completed != task )
        {
            cancellationToken.ThrowIfCancellationRequested();
        }

        return await task;
    }

    private static async Task WaitAsync( Task task, CancellationToken cancellationToken )
    {
        var completed = await Task.WhenAny( task, Task.Delay( Timeout.Infinite, cancellationToken ) );

        if ( completed != task )
        {
            cancellationToken.ThrowIfCancellationRequested();
        }

        await task;
    }

    private sealed class TestObserver : IDesignTimeNotificationObserver
    {
        private readonly Action<IDesignTimeNotificationEvent> _onEvent;

        public TestObserver( Action<IDesignTimeNotificationEvent> onEvent )
        {
            this._onEvent = onEvent;
        }

        public void OnEvent( IDesignTimeNotificationEvent notificationEvent ) => this._onEvent( notificationEvent );
    }
}
