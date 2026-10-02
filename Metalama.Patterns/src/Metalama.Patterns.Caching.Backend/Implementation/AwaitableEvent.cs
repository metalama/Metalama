// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using SharpCrafters.Common.Testing.Hooks;
using System.Collections;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Metalama.Patterns.Caching.Implementation;

// TODO: [Porting] Consider properly fixing all the warnings in AwaitableEvent.

// This file has many warnings. It is an internal utility class ported from PostSharp.Patterns.Common/Threading/Primitives.
// It's risky to modify code like this, and it's difficult to actually fix the warnings without changing the code, so
// for now, just suppress all the warnings.

// Resharper disable All
#pragma warning disable

// Ported from PostSharp.Patterns.Common/Threading/Primitives
internal sealed class AwaitableEvent
{
    private static readonly TimeSpan _infiniteTimeSpan = TimeSpan.FromMilliseconds( -1 );

    // ReSharper disable InconsistentNaming
    // states of the wait handle
    internal const int NOT_SIGNALED = 0;
    internal const int SIGNALED = 1;

    // states of each operation
    internal const int CREATED = 0;
    internal const int WAITING = 1;
    internal const int SUCCESS = 2;

    internal const int TIMEOUT = 3;

    // ReSharper restore InconsistentNaming

    private readonly int _resetMode;
    internal readonly WaitOperationQueue Operations;

    /// <summary>
    /// The optional test synchronization service, resolved once from the service provider. Never registered in
    /// production, in which case it stays <c>null</c> and every synchronization point is skipped. It is per-instance
    /// (rather than global mutable state) so that concurrent tests cannot interfere with each other.
    /// </summary>
    private readonly ITestSynchronizationProvider? _testSynchronizationProvider;

    /// <summary>
    /// The object that dispatches the continuation of a wait operation when the operation completes on a thread
    /// other than the one that awaits it. It is the thread pool unless the service provider supplies another one.
    /// </summary>
    private readonly IWorkItemDispatcher _workItemDispatcher;

    internal volatile int SignalState;

    public AwaitableEvent( EventResetMode resetMode, IServiceProvider? serviceProvider = null )
        : this( resetMode, false, serviceProvider ) { }

    public AwaitableEvent( EventResetMode resetMode, bool signaled, IServiceProvider? serviceProvider = null )
    {
        // Make sure that readonly field values are visible for other threads when we leave constructor.
        Volatile.Write( ref this._resetMode, (int) resetMode );
        Volatile.Write( ref this.Operations, new WaitOperationQueue() );

        this._testSynchronizationProvider = (ITestSynchronizationProvider?) serviceProvider?.GetService( typeof(ITestSynchronizationProvider) );
        this._workItemDispatcher = serviceProvider.GetWorkItemDispatcher();

        this.SignalState = signaled ? SIGNALED : NOT_SIGNALED;
    }

    /// <summary>
    /// Reaches a synchronization point, letting a test deterministically control the interleaving of this
    /// lock-free code. Costs a null check unless a test registered an <see cref="ITestSynchronizationProvider"/>,
    /// which never happens in production.
    /// </summary>
    private void SyncPoint( string name ) => this._testSynchronizationProvider?.SyncPoint( name );

    public void Wait( CancellationToken cancellationToken = default )
    {
        this.WaitInternal( _infiniteTimeSpan, cancellationToken );
    }

    public bool Wait( TimeSpan timeout, CancellationToken cancellationToken = default )
    {
        return this.WaitInternal( timeout, cancellationToken );
    }

    public Awaiter WaitAsync( CancellationToken cancellationToken = default )
    {
        return this.WaitOneAsyncInternal( _infiniteTimeSpan, cancellationToken );
    }

    public Awaiter WaitAsync( TimeSpan timeout, CancellationToken cancellationToken = default )
    {
        return this.WaitOneAsyncInternal( timeout, cancellationToken );
    }

    public Awaiter<TData> WaitAsync<TData>( CancellationToken cancellationToken = default )
    {
        return this.WaitOneAsyncInternal<TData>( _infiniteTimeSpan, cancellationToken );
    }

    public Awaiter<TData> WaitAsync<TData>( TimeSpan timeout, CancellationToken cancellationToken = default )
    {
        return this.WaitOneAsyncInternal<TData>( timeout, cancellationToken );
    }

    public void Set()
    {
        // we need to make sure that this works well with WaitOneInternal and WaitOneInternalAsync (and the rest of its workflow)
        // after we check the queue in ActivateOne, other thread may have enqueued a new operation and we need to make sure that either thread will process it
        // the invalid state would be if event was signaled and at the same time there was something in the queue

        this.SyncPoint( "Begin Set operation." );

        if ( this._resetMode == (int) EventResetMode.AutoReset )
        {
            this.SetAutoReset();
        }
        else
        {
            this.SetManualReset();
        }

        this.SyncPoint( "End Set operation." );
    }

    private void SetAutoReset()
    {
        while ( true )
        {
            // this cycle is potentially infinite

            WaitOperationBase op;

            if ( this.Operations.TryDequeue( out op ) )
            {
            HandleDequeuedOperation:
                this.SyncPoint( "Operation dequeued." );

                // note that this is not an infinite cycle - it will run at most three times (we have at most two state transitions possible)            
                var opState = op.State;

                if ( opState == SUCCESS || opState == TIMEOUT )
                {
                    this.SyncPoint( "Current operation was already finished or timed out, restarting." );

                    continue;
                }
                else
                {
                    if ( op.Activate() )
                    {
                        this.SyncPoint( "Operation activated, we can exit." );

                        break;
                    }
                    else
                    {
                        this.SyncPoint( "Other thread changed the operation state - try again." );

                        goto HandleDequeuedOperation;
                    }
                }
            }
            else
            {
                this.SyncPoint( "No WaitOne operation to activate." );

                // no operation is waiting, let's try to signal
                if ( NOT_SIGNALED == Interlocked.CompareExchange( ref this.SignalState, SIGNALED, NOT_SIGNALED ) )
                {
                    // signal successful
                    this.SyncPoint( "Signal set." );

                    // peek into queue for an operation
                    if ( this.Operations.TryPeek( out op ) )
                    {
                        this.SyncPoint( "Peeked an operation in queue, make sure that it is not waiting." );

                        // someone announced the waiting operation - now we need to determine their state
                        var opState = op.State;

                        if ( opState == WAITING )
                        {
                            // other thread may have missed our signal
                            if ( SIGNALED == Interlocked.CompareExchange( ref this.SignalState, NOT_SIGNALED, SIGNALED ) )
                            {
                                // we have our signal back, now we need to restart
                                this.SyncPoint( "Signal taken back, restart." );

                                continue;
                            }
                            else
                            {
                                // someone else took our signal, we can safely exit
                                this.SyncPoint( "Other thread took signal, exit." );

                                break;
                            }
                        }
                        else if ( opState == CREATED )
                        {
                            // if the operation is CREATED, it will notice our signal
                            this.SyncPoint( "Observed operation is CREATED, exit." );

                            break;
                        }
                        else
                        {
                            // if it is SUCCESS or TIMEOUT, it was finished by an other thread
                            // TODO: prove that at this point the following is not true: SIGNAL + operation in a waiting state
                            this.SyncPoint( "Observed operation is SUCCESS or TIMEOUT, exit." );

                            break;
                        }
                    }
                    else
                    {
                        // nothing in the queue, we can just safely exit
                        this.SyncPoint( "Observed empty queue, exit." );

                        break;
                    }
                }
                else
                {
                    // someone else signaled - we can safely exit
                    this.SyncPoint( "There is already a signal, exit." );

                    break;
                }
            }
        }
    }

    private void SetManualReset()
    {
        // set the signal
        this.SignalState = SIGNALED;

        // Full StoreLoad fence between publishing the signal and draining the queue. This is the signaler half
        // of a Dekker-style handshake with the waiter (which enqueues its operation and then reads the signal):
        // without it, a waiter that reads NOT_SIGNALED and parks a WAITING operation can be missed by this drain
        // (which reads a stale, empty queue), stranding the operation forever even though the signal is set. The
        // auto-reset path gets this fence for free from its Interlocked.CompareExchange on the signal; the plain
        // volatile store above does not provide it.
        Interlocked.MemoryBarrier();

        this.SyncPoint( "Signal set." );

        // now we need to go through the operation queue and activate until signal is reset
        // we don't care if there are multiple threads competing
        while ( true )
        {
            var mySignalState = this.SignalState;

            if ( mySignalState == NOT_SIGNALED )
            {
                break;
            }

            // activate one
            WaitOperationBase op;

            if ( this.Operations.TryDequeue( out op ) )
            {
            HandleDequeuedOperation:
                this.SyncPoint( "Operation dequeued." );

                // note that this is not an infinite cycle - it will run at most three times (we have at most two state transitions possible)            
                var opState = op.State;

                if ( opState == SUCCESS || opState == TIMEOUT )
                {
                    this.SyncPoint( "Current operation was already finished or timed out, move to the next one." );
                }
                else
                {
                    if ( !op.Activate() )
                    {
                        this.SyncPoint( "Other thread changed the operation state - try again." );

                        goto HandleDequeuedOperation;
                    }
                    else
                    {
                        this.SyncPoint( "Operation activated, move to the next one." );
                    }
                }
            }
            else
            {
                break;
            }
        }
    }

    public void Reset()
    {
        // Reset() only clears the flag; it does not touch the operation queue, and it is identical for both
        // reset modes.
        //
        // Concurrency requirement: Reset() is NOT safe to call concurrently with Set(). SetManualReset drains
        // the queue only while SignalState stays SIGNALED, so a Reset() that races a Set() can abort the drain
        // and strand operations that were already enqueued and WAITING before the Set() - a missed wakeup a
        // kernel ManualResetEvent would never produce. Likewise, a Reset() racing SetAutoReset's signal
        // take-back can make Set() conclude "another thread took the signal" and exit while a waiter keeps
        // waiting. Callers that rely on Set() reliably releasing current waiters (e.g. BackgroundTaskScheduler)
        // must serialize Set() and Reset() with respect to each other.
        this.SignalState = NOT_SIGNALED;
    }

    /// <summary>
    /// Creates the event a blocking wait parks on. Each wait operation gets its OWN event: it must never be
    /// shared or pooled.
    /// </summary>
    /// <remarks>
    /// This used to return a single <c>[ThreadStatic]</c> event reused by every wait on the thread, which made
    /// the event a contamination channel. <see cref="WaitOperationSync.Activate"/> cannot transition the state
    /// and set the event as one atomic step, so an activating thread preempted between the two can deliver its
    /// <c>Set()</c> after the waiter has already observed SUCCESS (via the timeout/cancellation or early-exit
    /// paths), returned, and started an unrelated wait - spuriously waking it. Stale operations left in the queue
    /// widened the same window to "until any future Set()". Giving each operation a private event makes a late
    /// <c>Set()</c> land on an object nobody waits on, which is harmless. Pooling would reintroduce reuse and the
    /// bug with it. The event is deliberately not disposed: a late <c>Set()</c> must not hit a disposed object,
    /// and this path only runs on blocking waits (in practice, scheduler dispose), so GC reclaim is cheap enough.
    /// </remarks>
    private static ManualResetEventSlim CreateWaitEvent() => new( false );

    private bool WaitInternal( TimeSpan timeout, CancellationToken cancellationToken )
    {
        this.SyncPoint( "Begin Wait operation." );

        try
        {
            // differentiate between zero timeout (sort of a peek) and some timeout (finite or infinite)
            if ( timeout == TimeSpan.Zero )
            {
                // we need to just peek if the handle is signaled (and consume the signal in case of auto reset event)
                // this does not race with Set/Reset as we do not work with the queue
                if ( this._resetMode == (int) EventResetMode.AutoReset )
                {
                    return this.NoWaitAutoReset();
                }
                else
                {
                    return this.NoWaitManualReset();
                }
            }
            else
            {
                if ( this._resetMode == (int) EventResetMode.AutoReset )
                {
                    return this.WaitAutoReset( timeout, cancellationToken );
                }
                else
                {
                    return this.WaitManualReset( timeout, cancellationToken );
                }
            }
        }
        finally
        {
            this.SyncPoint( "End Wait operation." );
        }
    }

    private bool NoWaitAutoReset()
    {
        if ( SIGNALED == Interlocked.CompareExchange( ref this.SignalState, NOT_SIGNALED, SIGNALED ) )
        {
            this.SyncPoint( "Signal consumed, return true." );

            return true;
        }
        else
        {
            this.SyncPoint( "Signal not consumed, return false." );

            return false;
        }
    }

    private bool NoWaitManualReset()
    {
        return this.SignalState == SIGNALED;
    }

    private bool WaitAutoReset( TimeSpan timeout, CancellationToken cancellationToken )
    {
        // AUTO RESET:
        // if the event is signaled, consume the signal and go through if successful
        if ( SIGNALED == Interlocked.CompareExchange( ref this.SignalState, NOT_SIGNALED, SIGNALED ) )
        {
            this.SyncPoint( "Signal consumed, return true." );

            return true;
        }

        // in case we did not obtain the signal, we need to enqueue an operation to let other threads help us
        var op =
            new WaitOperationSync()
            {
                State = CREATED,
                Event = CreateWaitEvent(), // private to this operation - never shared or pooled
                TestSynchronizationProvider = this._testSynchronizationProvider,
                WorkItemDispatcher = this._workItemDispatcher
            };

        // enqueue the operation (other threads will now see it)
        this.Operations.Enqueue( op );

        this.SyncPoint( "Enqueued operation." );

        if ( SIGNALED == Interlocked.CompareExchange( ref this.SignalState, NOT_SIGNALED, SIGNALED ) )
        {
            this.SyncPoint( "Signal taken, try to finish current operation." );

            if ( CREATED != Interlocked.CompareExchange( ref op.State, SUCCESS, CREATED ) )
            {
                this.SyncPoint( "Other thread finished the operation, use Set() to put signal back correctly." );
                this.Set();
            }

            this.SyncPoint( "Operation succeeded, return true." );

            return true;
        }
        else
        {
            this.SyncPoint( "Event is not signaled, begin to wait." );

            // try to announce that we are going to wait (other threads need to signal the event to get us going)
            if ( CREATED == Interlocked.CompareExchange( ref op.State, WAITING, CREATED ) )
            {
                this.SyncPoint( "Operation moved to waiting state, try to consume the signal again." );

                if ( SIGNALED == Interlocked.CompareExchange( ref this.SignalState, NOT_SIGNALED, SIGNALED ) )
                {
                    this.SyncPoint( "Signal taken, try to finish current operation." );

                    if ( WAITING != Interlocked.CompareExchange( ref op.State, SUCCESS, WAITING ) )
                    {
                        this.SyncPoint( "Other thread finished the operation, use Set() to put signal back correctly." );
                        this.Set();
                    }

                    this.SyncPoint( "Operation succeeded, return true." );

                    return true;
                }
                else
                {
                    this.SyncPoint( "Signal not taken, wait." );

                    bool signaled;

                    try
                    {
                        signaled = op.Event.Wait( timeout, cancellationToken );
                    }
                    catch ( OperationCanceledException )
                    {
                        // Withdraw the operation (WAITING -> TIMEOUT) and remove it from the queue, so that a later
                        // Set() neither activates it nor retains it. If the CAS loses, Activate() already delivered
                        // the signal to us (op is SUCCESS); for auto-reset we consumed it, so hand it back to another
                        // waiter before propagating.
                        if ( WAITING == Interlocked.CompareExchange( ref op.State, TIMEOUT, WAITING ) )
                        {
                            this.Operations.Remove( op );
                        }
                        else
                        {
                            Debug.Assert( op.State == SUCCESS );
                            this.Set();
                        }

                        throw;
                    }

                    if ( signaled )
                    {
                        // Activate() performed the WAITING -> SUCCESS transition and set the event.
                        Debug.Assert( op.State == SUCCESS );
                        this.SyncPoint( "Wait successful, return true." );

                        return true;
                    }
                    else
                    {
                        this.SyncPoint( "Wait timed out, going to timeout operation." );

                        if ( WAITING == Interlocked.CompareExchange( ref op.State, TIMEOUT, WAITING ) )
                        {
                            // Remove the withdrawn operation from the queue, so that a later Set() does not retain it.
                            this.Operations.Remove( op );
                            this.SyncPoint( "Operation timed out, return false." );

                            return false;
                        }
                        else
                        {
                            Debug.Assert( op.State == SUCCESS );

                            // we can presume that the operation was dequeued and end
                            this.SyncPoint( "Other thread finished the operation, return true." );

                            return true;
                        }
                    }
                }
            }
            else
            {
                Debug.Assert( op.State == SUCCESS );

                // we can presume that the operation was dequeued and end
                this.SyncPoint( "Other thread finished the operation, return true." );

                return true;
            }
        }
    }

    private bool WaitManualReset( TimeSpan timeout, CancellationToken cancellationToken )
    {
        // MANUAL RESET:
        // if the event is signaled, just go through
        if ( this.SignalState == SIGNALED )
        {
            this.SyncPoint( "Event is signaled, return true." );

            return true;
        }

        // event is not signaled
        var op =
            new WaitOperationSync()
            {
                State = CREATED,
                Event = CreateWaitEvent(), // private to this operation - never shared or pooled
                TestSynchronizationProvider = this._testSynchronizationProvider,
                WorkItemDispatcher = this._workItemDispatcher
            };

        // enqueue the operation (other threads will now see it)
        this.Operations.Enqueue( op );

        // Full StoreLoad fence between publishing the operation and the plain volatile read of the signal below
        // (waiter half of the Dekker-style handshake with SetManualReset). See the matching comment in
        // ScheduleContinuationInner.
        Interlocked.MemoryBarrier();

        this.SyncPoint( "Enqueued operation." );

        if ( this.SignalState == SIGNALED )
        {
            // we don't have to use CAS as we don't care if someone else finished our op before us
            op.State = SUCCESS;

            this.SyncPoint( "Event is signaled, moved operation to SUCCESS state, return true." );

            return true;
        }
        else
        {
            this.SyncPoint( "Event is not signaled, begin to wait." );

            if ( CREATED == Interlocked.CompareExchange( ref op.State, WAITING, CREATED ) )
            {
                this.SyncPoint( "Operation moved to waiting state, check the signal again." );

                if ( this.SignalState == SIGNALED )
                {
                    // we don't have to use CAS as we don't care if someone else finished our op before us
                    op.State = SUCCESS;

                    this.SyncPoint( "Event is signaled, moved operation to SUCCESS state, return true." );

                    return true;
                }
                else
                {
                    this.SyncPoint( "Signal not observed, wait." );

                    bool signaled;

                    try
                    {
                        signaled = op.Event.Wait( timeout, cancellationToken );
                    }
                    catch ( OperationCanceledException )
                    {
                        // Withdraw the operation (WAITING -> TIMEOUT) and remove it from the queue, so that a later
                        // Set() neither activates it nor retains it. Manual-reset stays SIGNALED, so there is no
                        // consumed signal to restore.
                        if ( WAITING == Interlocked.CompareExchange( ref op.State, TIMEOUT, WAITING ) )
                        {
                            this.Operations.Remove( op );
                        }

                        throw;
                    }

                    if ( signaled )
                    {
                        // Activate() performed the WAITING -> SUCCESS transition and set the event.
                        Debug.Assert( op.State == SUCCESS );
                        this.SyncPoint( "Wait successful, return true." );

                        return true;
                    }
                    else
                    {
                        this.SyncPoint( "Wait timed out, going to timeout operation." );

                        if ( WAITING == Interlocked.CompareExchange( ref op.State, TIMEOUT, WAITING ) )
                        {
                            // Remove the withdrawn operation from the queue, so that a later Set() does not retain it.
                            this.Operations.Remove( op );
                            this.SyncPoint( "Operation timed out, return false." );

                            return false;
                        }
                        else
                        {
                            Debug.Assert( op.State == SUCCESS );

                            // we can presume that the operation was dequeued and end
                            this.SyncPoint( "Other thread finished the operation, return true." );

                            return true;
                        }
                    }
                }
            }
            else
            {
                Debug.Assert( op.State == SUCCESS );

                // we can presume that the operation was dequeued and end
                this.SyncPoint( "Other thread finished the operation, return true." );

                return true;
            }
        }
    }

    private Awaiter WaitOneAsyncInternal( TimeSpan timeout, CancellationToken cancellationToken )
    {
        if ( timeout != TimeSpan.Zero && timeout != _infiniteTimeSpan )
        {
            throw new InvalidOperationException( "Support for non-zero finite timeout is not currently implemented." );
        }

        cancellationToken.ThrowIfCancellationRequested();

        if ( timeout == TimeSpan.Zero )
        {
            // this is a peek operation - we should finish immediately ( state machine will check IsCompleted before calling *OnCompleted )

            // we need to just peek if the handle is signaled (and consume the signal in case of auto reset event)
            // this does not race with Set/Reset as we do not work with the queue
            if ( this._resetMode == (int) EventResetMode.AutoReset )
            {
                if ( SIGNALED == Interlocked.CompareExchange( ref this.SignalState, NOT_SIGNALED, SIGNALED ) )
                {
                    this.SyncPoint( "Signal consumed, return awaiter with ImmediateResult=true." );

                    return new Awaiter( this, true );
                }
                else
                {
                    this.SyncPoint( "Signal not consumed, return awaiter with ImmediateResult=false." );

                    return new Awaiter( this, false );
                }
            }
            else
            {
                if ( this.SignalState == SIGNALED )
                {
                    this.SyncPoint( "Signal observed, return awaiter with ImmediateResult=true." );

                    return new Awaiter( this, true );
                }
                else
                {
                    this.SyncPoint( "Signal observed, return awaiter with ImmediateResult=false." );

                    return new Awaiter( this, false );
                }
            }
        }
        else
        {
            if ( this._resetMode == (int) EventResetMode.AutoReset )
            {
                // AUTO RESET:
                // if the event is signaled, consume the signal and go through if successful
                if ( SIGNALED == Interlocked.CompareExchange( ref this.SignalState, NOT_SIGNALED, SIGNALED ) )
                {
                    this.SyncPoint( "Signal consumed, return awaiter with ImmediateResult=true." );

                    return new Awaiter( this, true );
                }
            }
            else
            {
                // MANUAL RESET:
                if ( this.SignalState == SIGNALED )
                {
                    this.SyncPoint( "Signal observed, return true." );

                    return new Awaiter( this, true );
                }
            }

            // in case of both modes, if we could not end immediately, we need to return an awaiter and force the consumer to schedule a continuation
            // this awaiter will tell the state machine that the operation is not completed, forcing it to schedule a continuation
            var op =
                new WaitOperationAsync
                {
                    State = CREATED,
                    Timeout = timeout,
                    CancellationToken = cancellationToken,
                    TestSynchronizationProvider = this._testSynchronizationProvider,
                    WorkItemDispatcher = this._workItemDispatcher
                };

            // we cannot do more now because we don't have the continuation, we need to wait until it is set
            return new Awaiter( this, op );
        }
    }

    private Awaiter<TData> WaitOneAsyncInternal<TData>( TimeSpan timeout, CancellationToken cancellationToken )
    {
        if ( timeout != TimeSpan.Zero && timeout != _infiniteTimeSpan )
        {
            throw new InvalidOperationException( "Support for non-zero finite timeout is not currently implemented." );
        }

        cancellationToken.ThrowIfCancellationRequested();

        if ( timeout == TimeSpan.Zero )
        {
            // this is a peek operation - we should finish immediately ( state machine will check IsCompleted before calling *OnCompleted )

            // we need to just peek if the handle is signaled (and consume the signal in case of auto reset event)
            // this does not race with Set/Reset as we do not work with the queue
            if ( this._resetMode == (int) EventResetMode.AutoReset )
            {
                if ( SIGNALED == Interlocked.CompareExchange( ref this.SignalState, NOT_SIGNALED, SIGNALED ) )
                {
                    this.SyncPoint( "Signal consumed, return awaiter with ImmediateResult=true." );

                    return new Awaiter<TData>( this, true );
                }
                else
                {
                    this.SyncPoint( "Signal not consumed, return awaiter with ImmediateResult=false." );

                    return new Awaiter<TData>( this, false );
                }
            }
            else
            {
                if ( this.SignalState == SIGNALED )
                {
                    this.SyncPoint( "Signal observed, return awaiter with ImmediateResult=true." );

                    return new Awaiter<TData>( this, true );
                }
                else
                {
                    this.SyncPoint( "Signal observed, return awaiter with ImmediateResult=false." );

                    return new Awaiter<TData>( this, false );
                }
            }
        }
        else
        {
            if ( this._resetMode == (int) EventResetMode.AutoReset )
            {
                // AUTO RESET:
                // if the event is signaled, consume the signal and go through if successful
                if ( SIGNALED == Interlocked.CompareExchange( ref this.SignalState, NOT_SIGNALED, SIGNALED ) )
                {
                    this.SyncPoint( "Signal consumed, return awaiter with ImmediateResult=true." );

                    return new Awaiter<TData>( this, true );
                }
            }
            else
            {
                // MANUAL RESET:
                if ( this.SignalState == SIGNALED )
                {
                    this.SyncPoint( "Signal observed, return true." );

                    return new Awaiter<TData>( this, true );
                }
            }

            // in case of both modes, if we could not end immediately, we need to return an awaiter and force the consumer to schedule a continuation
            // this awaiter will tell the state machine that the operation is not completed, forcing it to schedule a continuation
            WaitOperationAsync<TData> op =
                new()
                {
                    State = CREATED,
                    Timeout = timeout,
                    CancellationToken = cancellationToken,
                    TestSynchronizationProvider = this._testSynchronizationProvider,
                    WorkItemDispatcher = this._workItemDispatcher
                };

            // we cannot do more now because we don't have the continuation, we need to wait until it is set
            return new Awaiter<TData>( this, op );
        }
    }

    internal void ScheduleContinuation( WaitOperationAsync op, Action continuation, bool flowContext )
    {
        // set the continuation (we need to call this after the wait is over)
        op.Continuation = continuation;
        op.TaskScheduler = TaskScheduler.Current;
        op.FlowContext = flowContext;

        this.ScheduleContinuationInner( op );
    }

    internal void ScheduleContinuation<TData>( WaitOperationAsync<TData> op, Action<WaitOperationAsync<TData>> continuation, bool flowContext )
    {
        // set the continuation (we need to call this after the wait is over)
        op.Continuation = continuation;
        op.TaskScheduler = TaskScheduler.Current;
        op.FlowContext = flowContext;

        this.ScheduleContinuationInner( op );
    }

    private void ScheduleContinuationInner( WaitOperationAsyncBase op )
    {
        // NOTE: at this point we cannot finish the operation synchronously
        //       we need to run Activate in order to continue the workflow

        // enqueue the operation (other threads will now see it)
        this.Operations.Enqueue( op );

        // Full StoreLoad fence between publishing the operation and reading the signal below (the waiter half of
        // the Dekker-style handshake with Set()). The manual-reset branch reads the signal with a plain volatile
        // read; without this fence a Set() that runs concurrently can be missed by both sides, stranding the
        // operation in the WAITING state. The auto-reset branch re-reads the signal with an Interlocked operation
        // and does not depend on this fence, but it is harmless there.
        Interlocked.MemoryBarrier();

        this.SyncPoint( "Enqueued operation." );

        if ( this._resetMode == (int) EventResetMode.AutoReset )
        {
            if ( SIGNALED == Interlocked.CompareExchange( ref this.SignalState, NOT_SIGNALED, SIGNALED ) )
            {
                this.SyncPoint( "Signal taken, try to finish current operation." );

                if ( CREATED != Interlocked.CompareExchange( ref op.State, SUCCESS, CREATED ) )
                {
                    this.SyncPoint( "Other thread finished the operation, use Set() to put signal back correctly." );
                    this.Set();
                }

                this.SyncPoint( "Operation succeeded, activate and exit." );
                op.Activate();
            }
            else
            {
                this.SyncPoint( "Event is not signaled, begin to wait." );

                // try to announce that we are going to wait (other threads need to signal the event to get us going)
                if ( CREATED == Interlocked.CompareExchange( ref op.State, WAITING, CREATED ) )
                {
                    this.SyncPoint( "Operation moved to waiting state, try to consume the signal again." );

                    if ( SIGNALED == Interlocked.CompareExchange( ref this.SignalState, NOT_SIGNALED, SIGNALED ) )
                    {
                        this.SyncPoint( "Signal taken, try to finish current operation." );

                        if ( WAITING == Interlocked.CompareExchange( ref op.State, SUCCESS, WAITING ) )
                        {
                            this.SyncPoint( "Operation succeeded, activate and exit." );
                            op.Activate();
                        }
                        else
                        {
                            this.SyncPoint( "Other thread finished the operation, use Set() to put signal back correctly." );
                            this.Set();
                        }
                    }
                    else
                    {
                        this.SyncPoint( "Signal not taken, wait." );

                        if ( op.Timeout == _infiniteTimeSpan )
                        {
                            // Set() activates the operation. If the token is cancelled first, the operation is withdrawn instead.
                            op.RegisterCancellation();
                        }
                        else
                        {
                            if ( op.CancellationToken != CancellationToken.None )
                            {
                                throw new NotImplementedException( "Finite waiting with cancellation token is not implemented." );
                            }
                            else
                            {
                                throw new NotImplementedException( "Finite waiting is not implemented." );
                            }
                        }
                    }
                }
                else
                {
                    Debug.Assert( op.State == SUCCESS );
                    this.SyncPoint( "Other thread finished the operation, activate operation and exit." );
                    op.Activate();
                }
            }
        }
        else
        {
            if ( this.SignalState == SIGNALED )
            {
                this.SyncPoint( "Event is signaled, try to finish the operation." );

                if ( CREATED == Interlocked.CompareExchange( ref op.State, SUCCESS, CREATED ) )
                {
                    this.SyncPoint( "Finished the operation, activate and exit." );
                    op.Activate();
                }
                else
                {
                    this.SyncPoint( "Other thread finished the operation, exit." );
                }
            }
            else
            {
                this.SyncPoint( "Event is not signaled, begin to wait." );

                if ( CREATED == Interlocked.CompareExchange( ref op.State, WAITING, CREATED ) )
                {
                    this.SyncPoint( "Operation moved to waiting state, check the signal again." );

                    if ( this.SignalState == SIGNALED )
                    {
                        this.SyncPoint( "Event is signaled, try to finish the operation." );

                        // The operation is in the WAITING state here (we just set it above), so the transition
                        // to complete it is WAITING->SUCCESS, matching the auto-reset branch. (Comparing against
                        // CREATED always failed, leaving self-activation to Set()'s queue drain.)
                        if ( WAITING == Interlocked.CompareExchange( ref op.State, SUCCESS, WAITING ) )
                        {
                            this.SyncPoint( "Finished the operation, activate and exit." );
                            op.Activate();
                        }
                        else
                        {
                            this.SyncPoint( "Other thread finished the operation, exit." );
                        }
                    }
                    else
                    {
                        this.SyncPoint( "Signal not taken, wait." );

                        if ( op.Timeout == _infiniteTimeSpan )
                        {
                            // Set() activates the operation. If the token is cancelled first, the operation is withdrawn instead.
                            op.RegisterCancellation();
                        }
                        else
                        {
                            if ( op.CancellationToken != CancellationToken.None )
                            {
                                throw new NotImplementedException( "Finite waiting with cancellation token is not implemented." );
                            }
                            else
                            {
                                throw new NotImplementedException( "Finite waiting is not implemented." );
                            }
                        }
                    }
                }
                else
                {
                    Debug.Assert( op.State == SUCCESS );
                    this.SyncPoint( "Other thread finished the operation, activate operation and exit." );
                    op.Activate();
                }
            }
        }
    }

    internal abstract class WaitOperationBase
    {
        // operations begins in CREATED state
        // CREATED -> WAITING is done by Wait
        // CREATED -> SUCCESS can be done by both Wait and Set; in synchronous Wait, Wait will exit without wait; in asynchronous Wait, Wait will Activate it's continuation
        // WAITING -> TIMEOUT is done by Wait
        // WAITING -> SUCCESS is done by both Wait and Set; operation that does this transition is responsible for activation
        public volatile int State;

        // Copied from the owning AwaitableEvent when the operation is created, so that Activate() - which runs
        // without a reference to the event - can reach synchronization points too. Null in production.
        public ITestSynchronizationProvider? TestSynchronizationProvider;

        // Copied from the owning AwaitableEvent for the same reason as TestSynchronizationProvider.
        public IWorkItemDispatcher WorkItemDispatcher = ThreadPoolWorkItemDispatcher.Instance;

        // The queue in which the operation was enqueued, or null if the operation was never enqueued.
        public WaitOperationQueue? Queue;

        // The node of the operation in Queue, or null if the operation is not in the queue. Accessed only under the
        // lock of the queue.
        public LinkedListNode<WaitOperationBase>? QueueNode;

        /// <inheritdoc cref="AwaitableEvent.SyncPoint"/>
        protected void SyncPoint( string name ) => this.TestSynchronizationProvider?.SyncPoint( name );

        public abstract bool Activate();
    }

    /// <summary>
    /// The first-in, first-out queue of the wait operations of an <see cref="AwaitableEvent"/>. Unlike
    /// <see cref="ConcurrentQueue{T}"/>, it supports the removal of an operation from any position.
    /// </summary>
    /// <remarks>
    /// Each method is atomic with respect to the other methods, as the methods of <see cref="ConcurrentQueue{T}"/> are.
    /// The methods take a lock, which is also a full memory barrier. An operation can be enqueued only once.
    /// </remarks>
    internal sealed class WaitOperationQueue : IEnumerable<WaitOperationBase>
    {
        private readonly LinkedList<WaitOperationBase> _list = new();

        public bool IsEmpty
        {
            get
            {
                lock ( this._list )
                {
                    return this._list.Count == 0;
                }
            }
        }

        public int Count
        {
            get
            {
                lock ( this._list )
                {
                    return this._list.Count;
                }
            }
        }

        public void Enqueue( WaitOperationBase operation )
        {
            lock ( this._list )
            {
                operation.Queue = this;
                operation.QueueNode = this._list.AddLast( operation );
            }
        }

        public bool TryDequeue( out WaitOperationBase operation )
        {
            lock ( this._list )
            {
                var node = this._list.First;

                if ( node == null )
                {
                    operation = null;

                    return false;
                }

                this._list.RemoveFirst();
                operation = node.Value;
                operation.QueueNode = null;

                return true;
            }
        }

        public bool TryPeek( out WaitOperationBase operation )
        {
            lock ( this._list )
            {
                operation = this._list.First?.Value;

                return operation != null;
            }
        }

        /// <summary>
        /// Removes an operation from the queue if it is still in the queue.
        /// </summary>
        /// <returns><c>true</c> if the operation was removed, or <c>false</c> if it had already been dequeued.</returns>
        public bool Remove( WaitOperationBase operation )
        {
            lock ( this._list )
            {
                var node = operation.QueueNode;

                if ( node == null )
                {
                    return false;
                }

                this._list.Remove( node );
                operation.QueueNode = null;

                return true;
            }
        }

        /// <summary>
        /// Returns an enumerator over a snapshot of the queue.
        /// </summary>
        public IEnumerator<WaitOperationBase> GetEnumerator()
        {
            lock ( this._list )
            {
                return new List<WaitOperationBase>( this._list ).GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }

    internal class WaitOperationSync : WaitOperationBase
    {
        // synchronization of blocking wait
        public volatile ManualResetEventSlim Event;

        public override bool Activate()
        {
            // Participate in the state protocol like the async operations (see the state comment on
            // WaitOperationBase): the wakeup is owned by whoever performs the CREATED/WAITING -> SUCCESS
            // transition. We win only by doing that transition, and only then do we release the waiter.
            //
            // If the operation already completed (SUCCESS) or was withdrawn on timeout/cancellation (TIMEOUT),
            // we lose and return false. Returning false (rather than the old unconditional true) is what lets
            // Set()'s drain loop move on without consuming the signal for a dead operation - critical for
            // auto-reset - and prevents us from calling Set() on the shared thread-static event of a stale
            // operation, which would spuriously wake an unrelated wait on that thread.
            var state = this.State;

            if ( state != CREATED && state != WAITING )
            {
                return false;
            }

            if ( state == Interlocked.CompareExchange( ref this.State, SUCCESS, state ) )
            {
                this.Event.Set();

                return true;
            }

            // Lost the CAS: the waiter concurrently timed out or was cancelled.
            return false;
        }
    }

    internal abstract class WaitOperationAsyncBase : WaitOperationBase
    {
        // needed only in case of awaitable wait, for blocking wait it is just informational
        public TimeSpan Timeout;

        // task scheduler that was current when continuation was scheduled
        public volatile TaskScheduler TaskScheduler;

        // flow context information
        public volatile bool FlowContext;

        // token received for wait cancellation
        public CancellationToken CancellationToken;

        private static readonly Action<object> _cancelCallback = state => ((WaitOperationAsyncBase) state).Cancel();

        /// <summary>
        /// The registration of the cancellation callback while the operation is WAITING, or <c>null</c>.
        /// </summary>
        /// <remarks>
        /// The registration is stored boxed so that it can be exchanged atomically. It is disposed when the operation is
        /// activated, so that a long-lived token does not keep the operation and its continuation alive.
        /// </remarks>
        private IDisposable? _cancellationRegistration;

        /// <summary>
        /// Registers a callback that withdraws the operation when <see cref="CancellationToken"/> is cancelled.
        /// Called after the operation moved to the WAITING state.
        /// </summary>
        public void RegisterCancellation()
        {
            if ( !this.CancellationToken.CanBeCanceled )
            {
                return;
            }

            // If the token is already cancelled, the callback runs synchronously.
            IDisposable registration = this.CancellationToken.Register( _cancelCallback, this );

            Interlocked.Exchange( ref this._cancellationRegistration, registration );

            // Activate() may have run before the registration was stored, in which case it could not dispose it.
            if ( this.State != WAITING )
            {
                this.DisposeCancellationRegistration();
            }
        }

        /// <summary>
        /// Disposes the registration of the cancellation callback, if it has not been disposed yet.
        /// </summary>
        protected void DisposeCancellationRegistration()
        {
            Interlocked.Exchange( ref this._cancellationRegistration, null )?.Dispose();
        }

        /// <summary>
        /// Withdraws the operation when its <see cref="CancellationToken"/> is cancelled.
        /// </summary>
        private void Cancel()
        {
            // Withdraw the operation (WAITING -> TIMEOUT) so that a Set() that has already dequeued it skips it and
            // does not consume the signal. Then remove the operation from the queue, so that the queue does not
            // retain the operation, its continuation and its token until the next Set(). If the CAS fails,
            // Activate() already moved the operation to SUCCESS and scheduled the continuation.
            if ( WAITING == Interlocked.CompareExchange( ref this.State, TIMEOUT, WAITING ) )
            {
                this.Queue?.Remove( this );
                this.SyncPoint( "Operation cancelled, schedule continuation." );
                this.ScheduleContinuation();
            }
        }

        /// <summary>
        /// Throws an <see cref="OperationCanceledException"/> if the operation was withdrawn because its
        /// <see cref="CancellationToken"/> was cancelled.
        /// </summary>
        public void ThrowIfCancelled()
        {
            // An asynchronous operation supports only an infinite timeout, so it reaches the TIMEOUT state only
            // through Cancel().
            if ( this.State == TIMEOUT )
            {
                throw new OperationCanceledException( this.CancellationToken );
            }
        }

        /// <summary>
        /// Schedules the continuation, at most once.
        /// </summary>
        protected abstract void ScheduleContinuation();
    }

    internal sealed class WaitOperationAsync : WaitOperationAsyncBase
    {
        // caching delegate
        private static readonly WaitCallback _runContinuationWaitCallback = RunContinuation;

        // continuation
        public volatile Action Continuation;

        // 0 until the continuation has been scheduled, then 1. Guarantees the continuation is scheduled at most once.
        private int _continuationScheduled;

        public override bool Activate()
        {
            var state = this.State;

            if ( state == TIMEOUT )
            {
                // The operation was withdrawn by cancellation.
                return false;
            }

            if ( state == SUCCESS )
            {
                this.SyncPoint( "Operation already in SUCCESS state." );
            }
            else if ( state == Interlocked.CompareExchange( ref this.State, SUCCESS, state ) )
            {
                if ( state == CREATED )
                {
                    this.SyncPoint( "Operation moved to SUCCESS state, it was CREATED." );
                }
                else
                {
                    this.SyncPoint( "Operation moved to SUCCESS state, it was WAITING, schedule continuation." );
                }
            }
            else
            {
                return false;
            }

            this.DisposeCancellationRegistration();
            this.ScheduleContinuation();

            return true;
        }

        protected override void ScheduleContinuation()
        {
            // Activate() can legitimately be reached more than once for the same operation (e.g. Set() activates
            // the enqueued operation while the scheduling thread also calls Activate() after its now-stale CAS).
            // The continuation must be scheduled exactly once: scheduling it twice re-runs an already-completed
            // async state machine ("attempt to transition a task to a final state when it had already completed").
            if ( Interlocked.CompareExchange( ref this._continuationScheduled, 1, 0 ) != 0 )
            {
                return;
            }

            // NOTE: this is how YieldAwaiter handles reactivation, but we omit SynchronizationContext.CurrentNoFlow which is internal
            // TODO: make sure that this reactivation algorithm is correct

            if ( this.TaskScheduler != TaskScheduler.Default )
            {
                Task.Factory.StartNew( this.Continuation, default, TaskCreationOptions.PreferFairness, this.TaskScheduler );
            }
            else if ( this.FlowContext )
            {
                this.WorkItemDispatcher.Dispatch( _runContinuationWaitCallback, this.Continuation );
            }
            else
            {
                this.WorkItemDispatcher.Dispatch( _runContinuationWaitCallback, this.Continuation, false );
            }
        }

        public static void RunContinuation( object state )
        {
            var action = (Action) state;
            action();
        }
    }

    internal sealed class WaitOperationAsync<TData> : WaitOperationAsyncBase
    {
        // caching delegates
        // ReSharper disable StaticMemberInGenericType
        private static readonly Action<object> _runContinuationAction = RunContinuation;

        private static readonly WaitCallback _runContinuationWaitCallback = RunContinuation;

        // ReSharper restore StaticMemberInGenericType

        // continuation
        public volatile Action<WaitOperationAsync<TData>> Continuation;

        // This needs to be a field (and not a property) because TData may be a mutable struct.
        public TData Data;

        // 0 until the continuation has been scheduled, then 1. Guarantees the continuation is scheduled at most once.
        private int _continuationScheduled;

        /// <summary>
        /// Gets a value indicating whether the wait succeeded. Throws an <see cref="OperationCanceledException"/> if the
        /// operation was withdrawn because its <see cref="WaitOperationAsyncBase.CancellationToken"/> was cancelled,
        /// so that a continuation that reads the operation it receives observes the cancellation.
        /// </summary>
        public bool Result
        {
            get
            {
                this.ThrowIfCancelled();

                return this.State == SUCCESS;
            }
        }

        public override bool Activate()
        {
            var state = this.State;

            if ( state == TIMEOUT )
            {
                // The operation was withdrawn by cancellation.
                return false;
            }

            if ( state == SUCCESS )
            {
                this.SyncPoint( "Operation already in SUCCESS state." );
            }
            else if ( state == Interlocked.CompareExchange( ref this.State, SUCCESS, state ) )
            {
                if ( state == CREATED )
                {
                    this.SyncPoint( "Operation moved to SUCCESS state, it was CREATED." );
                }
                else
                {
                    this.SyncPoint( "Operation moved to SUCCESS state, it was WAITING, schedule continuation." );
                }
            }
            else
            {
                return false;
            }

            this.DisposeCancellationRegistration();
            this.ScheduleContinuation();

            return true;
        }

        protected override void ScheduleContinuation()
        {
            // Activate() can legitimately be reached more than once for the same operation (e.g. Set() activates
            // the enqueued operation while the scheduling thread also calls Activate() after its now-stale CAS).
            // The continuation must be scheduled exactly once: scheduling it twice re-runs an already-completed
            // async state machine ("attempt to transition a task to a final state when it had already completed").
            if ( Interlocked.CompareExchange( ref this._continuationScheduled, 1, 0 ) != 0 )
            {
                return;
            }

            // NOTE: this is how YieldAwaiter handles reactivation, but we omit SynchronizationContext.CurrentNoFlow which is internal
            // TODO: make sure that this reactivation algorithm is correct

            if ( this.TaskScheduler != TaskScheduler.Default )
            {
                Task.Factory.StartNew(
                    _runContinuationAction,
                    this,
                    default,
                    TaskCreationOptions.PreferFairness,
                    this.TaskScheduler );
            }
            else if ( this.FlowContext )
            {
                this.WorkItemDispatcher.Dispatch( _runContinuationWaitCallback, this );
            }
            else
            {
                this.WorkItemDispatcher.Dispatch( _runContinuationWaitCallback, this, false );
            }
        }

        public static void RunContinuation( object state )
        {
            WaitOperationAsync<TData> operation = (WaitOperationAsync<TData>) state;
            operation.Continuation( operation );
        }
    }

    [EditorBrowsable( EditorBrowsableState.Never )]
    public readonly struct Awaiter : ICriticalNotifyCompletion
    {
        private readonly AwaitableEvent _owner;

        private readonly WaitOperationAsync _operation;

        private readonly bool? _immediateResult;

        public Awaiter( AwaitableEvent owner, bool immediateResult )
        {
            this._owner = owner;
            this._operation = null;
            this._immediateResult = immediateResult;
        }

        public Awaiter( AwaitableEvent owner, WaitOperationAsync operation )
        {
            this._operation = operation;
            this._owner = owner;
            this._immediateResult = null;
        }

        public bool IsCompleted
        {
            get { return this._immediateResult != null; }
        }

        public void OnCompleted( Action continuation )
        {
            // Only reachable when IsCompleted is false, i.e. this is a real (non-immediate) awaiter.
            Debug.Assert( this._operation != null, "OnCompleted called on an already-completed awaiter." );
            this._owner.ScheduleContinuation( this._operation, continuation, true );
        }

        public void UnsafeOnCompleted( Action continuation )
        {
            Debug.Assert( this._operation != null, "UnsafeOnCompleted called on an already-completed awaiter." );
            this._owner.ScheduleContinuation( this._operation, continuation, false );
        }

        public bool GetResult()
        {
            if ( this._immediateResult != null )
            {
                return this._immediateResult.Value;
            }

            this._operation.ThrowIfCancelled();

            return this._operation.State == SUCCESS;
        }

        public Awaiter GetAwaiter()
        {
            return this;
        }
    }

    // NOTE: while this looks like awaitable it cannot be awaited from state machine as state machine does not support continuation with argument
    //       we keep the name and pattern to make the code that uses it familar for users
    [EditorBrowsable( EditorBrowsableState.Never )]
    public readonly struct Awaiter<TData>
    {
        private readonly AwaitableEvent _owner;

        internal readonly WaitOperationAsync<TData> Operation;

        private readonly bool? _immediateResult;

        public Awaiter( AwaitableEvent owner, bool immediateResult )
        {
            this._owner = owner;
            this.Operation = null;
            this._immediateResult = immediateResult;
        }

        public Awaiter( AwaitableEvent owner, WaitOperationAsync<TData> operation )
        {
            this.Operation = operation;
            this._owner = owner;
            this._immediateResult = null;
        }

        public bool IsCompleted
        {
            get { return this._immediateResult != null; }
        }

        public void OnCompleted( Action<WaitOperationAsync<TData>> continuation )
        {
            Debug.Assert( this.Operation != null, "OnCompleted called on an already-completed awaiter." );
            this._owner.ScheduleContinuation( this.Operation, continuation, true );
        }

        public void UnsafeOnCompleted( Action<WaitOperationAsync<TData>> continuation )
        {
            Debug.Assert( this.Operation != null, "UnsafeOnCompleted called on an already-completed awaiter." );
            this._owner.ScheduleContinuation( this.Operation, continuation, false );
        }

        public bool GetResult()
        {
            if ( this._immediateResult != null )
            {
                return this._immediateResult.Value;
            }

            this.Operation.ThrowIfCancelled();

            return this.Operation.State == SUCCESS;
        }

        public Awaiter<TData> GetAwaiter()
        {
            return this;
        }

        public TData Data { get { return this.Operation.Data; } set { this.Operation.Data = value; } }
    }
}