// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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
    internal readonly ConcurrentQueue<WaitOperationBase> Operations;

    internal volatile int SignalState;

    [ThreadStatic]
    private static volatile ManualResetEventSlim _threadLocalEvent;

    public AwaitableEvent( EventResetMode resetMode )
        : this( resetMode, false ) { }

    public AwaitableEvent( EventResetMode resetMode, bool signaled )
    {
        // Make sure that readonly field values are visible for other threads when we leave constructor.
        Volatile.Write( ref this._resetMode, (int) resetMode );
        Volatile.Write( ref this.Operations, new ConcurrentQueue<WaitOperationBase>() );

        this.SignalState = signaled ? SIGNALED : NOT_SIGNALED;
    }

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

        ConcurrencyTestingApi.TraceEvent( "Begin Set operation." );

        if ( this._resetMode == (int) EventResetMode.AutoReset )
        {
            this.SetAutoReset();
        }
        else
        {
            this.SetManualReset();
        }

        ConcurrencyTestingApi.TraceEvent( "End Set operation." );
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
                ConcurrencyTestingApi.TraceEvent( "Operation dequeued." );

                // note that this is not an infinite cycle - it will run at most three times (we have at most two state transitions possible)            
                var opState = op.State;

                if ( opState == SUCCESS || opState == TIMEOUT )
                {
                    ConcurrencyTestingApi.TraceEvent( "Current operation was already finished or timed out, restarting." );

                    continue;
                }
                else
                {
                    if ( op.Activate() )
                    {
                        ConcurrencyTestingApi.TraceEvent( "Operation activated, we can exit." );

                        break;
                    }
                    else
                    {
                        ConcurrencyTestingApi.TraceEvent( "Other thread changed the operation state - try again." );

                        goto HandleDequeuedOperation;
                    }
                }
            }
            else
            {
                ConcurrencyTestingApi.TraceEvent( "No WaitOne operation to activate." );

                // no operation is waiting, let's try to signal
                if ( NOT_SIGNALED == Interlocked.CompareExchange( ref this.SignalState, SIGNALED, NOT_SIGNALED ) )
                {
                    // signal successful
                    ConcurrencyTestingApi.TraceEvent( "Signal set." );

                    // peek into queue for an operation
                    if ( this.Operations.TryPeek( out op ) )
                    {
                        ConcurrencyTestingApi.TraceEvent( "Peeked an operation in queue, make sure that it is not waiting." );

                        // someone announced the waiting operation - now we need to determine their state
                        var opState = op.State;

                        if ( opState == WAITING )
                        {
                            // other thread may have missed our signal
                            if ( SIGNALED == Interlocked.CompareExchange( ref this.SignalState, NOT_SIGNALED, SIGNALED ) )
                            {
                                // we have our signal back, now we need to restart
                                ConcurrencyTestingApi.TraceEvent( "Signal taken back, restart." );

                                continue;
                            }
                            else
                            {
                                // someone else took our signal, we can safely exit
                                ConcurrencyTestingApi.TraceEvent( "Other thread took signal, exit." );

                                break;
                            }
                        }
                        else if ( opState == CREATED )
                        {
                            // if the operation is CREATED, it will notice our signal
                            ConcurrencyTestingApi.TraceEvent( "Observed operation is CREATED, exit." );

                            break;
                        }
                        else
                        {
                            // if it is SUCCESS or TIMEOUT, it was finished by an other thread
                            // TODO: prove that at this point the following is not true: SIGNAL + operation in a waiting state
                            ConcurrencyTestingApi.TraceEvent( "Observed operation is SUCCESS or TIMEOUT, exit." );

                            break;
                        }
                    }
                    else
                    {
                        // nothing in the queue, we can just safely exit
                        ConcurrencyTestingApi.TraceEvent( "Observed empty queue, exit." );

                        break;
                    }
                }
                else
                {
                    // someone else signaled - we can safely exit
                    ConcurrencyTestingApi.TraceEvent( "There is already a signal, exit." );

                    break;
                }
            }
        }
    }

    private void SetManualReset()
    {
        // set the signal
        this.SignalState = SIGNALED;

        ConcurrencyTestingApi.TraceEvent( "Signal set." );

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
                ConcurrencyTestingApi.TraceEvent( "Operation dequeued." );

                // note that this is not an infinite cycle - it will run at most three times (we have at most two state transitions possible)            
                var opState = op.State;

                if ( opState == SUCCESS || opState == TIMEOUT )
                {
                    ConcurrencyTestingApi.TraceEvent( "Current operation was already finished or timed out, move to the next one." );
                }
                else
                {
                    if ( !op.Activate() )
                    {
                        ConcurrencyTestingApi.TraceEvent( "Other thread changed the operation state - try again." );

                        goto HandleDequeuedOperation;
                    }
                    else
                    {
                        ConcurrencyTestingApi.TraceEvent( "Operation activated, move tot he next one." );
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
        if ( this._resetMode == (int) EventResetMode.AutoReset )
        {
            // TODO: does this work correctly with Set()?
            this.SignalState = NOT_SIGNALED;
        }
        else
        {
            this.SignalState = NOT_SIGNALED;
        }
    }

    private static ManualResetEventSlim GetThreadLocalEvent()
    {
        if ( _threadLocalEvent == null )
        {
            _threadLocalEvent = new ManualResetEventSlim( false );
        }
        else
        {
            _threadLocalEvent.Reset();
        }

        return _threadLocalEvent;
    }

    private bool WaitInternal( TimeSpan timeout, CancellationToken cancellationToken )
    {
        ConcurrencyTestingApi.TraceEvent( "Begin Wait operation." );

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
            ConcurrencyTestingApi.TraceEvent( "End Wait operation." );
        }
    }

    private bool NoWaitAutoReset()
    {
        if ( SIGNALED == Interlocked.CompareExchange( ref this.SignalState, NOT_SIGNALED, SIGNALED ) )
        {
            ConcurrencyTestingApi.TraceEvent( "Signal consumed, return true." );

            return true;
        }
        else
        {
            ConcurrencyTestingApi.TraceEvent( "Signal not consumed, return false." );

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
            ConcurrencyTestingApi.TraceEvent( "Signal consumed, return true." );

            return true;
        }

        // in case we did not obtain the signal, we need to enqueue an operation to let other threads help us
        var op =
            new WaitOperationSync()
            {
                State = CREATED, Event = GetThreadLocalEvent() // optimize this using one thread-static event
            };

        // enqueue the operation (other threads will now see it)
        this.Operations.Enqueue( op );

        ConcurrencyTestingApi.TraceEvent( "Enqueued operation." );

        if ( SIGNALED == Interlocked.CompareExchange( ref this.SignalState, NOT_SIGNALED, SIGNALED ) )
        {
            ConcurrencyTestingApi.TraceEvent( "Signal taken, try to finish current operation." );

            if ( CREATED != Interlocked.CompareExchange( ref op.State, SUCCESS, CREATED ) )
            {
                ConcurrencyTestingApi.TraceEvent( "Other thread finished the operation, use Set() to put signal back correctly." );
                this.Set();
            }

            ConcurrencyTestingApi.TraceEvent( "Operation succeeded, return true." );

            return true;
        }
        else
        {
            ConcurrencyTestingApi.TraceEvent( "Event is not signaled, begin to wait." );

            // try to announce that we are going to wait (other threads need to signal the event to get us going)
            if ( CREATED == Interlocked.CompareExchange( ref op.State, WAITING, CREATED ) )
            {
                ConcurrencyTestingApi.TraceEvent( "Operation moved to waiting state, try to consume the signal again." );

                if ( SIGNALED == Interlocked.CompareExchange( ref this.SignalState, NOT_SIGNALED, SIGNALED ) )
                {
                    ConcurrencyTestingApi.TraceEvent( "Signal taken, try to finish current operation." );

                    if ( WAITING != Interlocked.CompareExchange( ref op.State, SUCCESS, WAITING ) )
                    {
                        ConcurrencyTestingApi.TraceEvent( "Other thread finished the operation, use Set() to put signal back correctly." );
                        this.Set();
                    }

                    ConcurrencyTestingApi.TraceEvent( "Operation succeeded, return true." );

                    return true;
                }
                else
                {
                    ConcurrencyTestingApi.TraceEvent( "Signal not taken, wait." );

                    if ( op.Event.Wait( timeout, cancellationToken ) )
                    {
                        Debug.Assert( op.State == WAITING );
                        op.State = SUCCESS;
                        ConcurrencyTestingApi.TraceEvent( "Wait successful, return true." );

                        return true;
                    }
                    else
                    {
                        ConcurrencyTestingApi.TraceEvent( "Wait timed out, going to timeout operation." );

                        if ( WAITING == Interlocked.CompareExchange( ref op.State, TIMEOUT, WAITING ) )
                        {
                            ConcurrencyTestingApi.TraceEvent( "Operation timed out, return false." );

                            return false;
                        }
                        else
                        {
                            Debug.Assert( op.State == SUCCESS );

                            // we can presume that the operation was dequeued and end
                            ConcurrencyTestingApi.TraceEvent( "Other thread finished the operation, return true." );

                            return true;
                        }
                    }
                }
            }
            else
            {
                Debug.Assert( op.State == SUCCESS );

                // we can presume that the operation was dequeued and end
                ConcurrencyTestingApi.TraceEvent( "Other thread finished the operation, return true." );

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
            ConcurrencyTestingApi.TraceEvent( "Event is signaled, return true." );

            return true;
        }

        // event is not signaled
        var op =
            new WaitOperationSync()
            {
                State = CREATED, Event = GetThreadLocalEvent() // optimize this using one thread-static event
            };

        // enqueue the operation (other threads will now see it)
        this.Operations.Enqueue( op );

        ConcurrencyTestingApi.TraceEvent( "Enqueued operation." );

        if ( this.SignalState == SIGNALED )
        {
            // we don't have to use CAS as we don't care if someone else finished our op before us
            op.State = SUCCESS;

            ConcurrencyTestingApi.TraceEvent( "Event is signaled, moved operation to SUCCESS state, return true." );

            return true;
        }
        else
        {
            ConcurrencyTestingApi.TraceEvent( "Event is not signaled, begin to wait." );

            if ( CREATED == Interlocked.CompareExchange( ref op.State, WAITING, CREATED ) )
            {
                ConcurrencyTestingApi.TraceEvent( "Operation moved to waiting state, check the signal again." );

                if ( this.SignalState == SIGNALED )
                {
                    // we don't have to use CAS as we don't care if someone else finished our op before us
                    op.State = SUCCESS;

                    ConcurrencyTestingApi.TraceEvent( "Event is signaled, moved operation to SUCCESS state, return true." );

                    return true;
                }
                else
                {
                    ConcurrencyTestingApi.TraceEvent( "Signal not observed, wait." );

                    if ( op.Event.Wait( timeout, cancellationToken ) )
                    {
                        Debug.Assert( op.State == WAITING );
                        op.State = SUCCESS;
                        ConcurrencyTestingApi.TraceEvent( "Wait successful, return true." );

                        return true;
                    }
                    else
                    {
                        ConcurrencyTestingApi.TraceEvent( "Wait timed out, going to timeout operation." );

                        if ( WAITING == Interlocked.CompareExchange( ref op.State, TIMEOUT, WAITING ) )
                        {
                            ConcurrencyTestingApi.TraceEvent( "Operation timed out, return false." );

                            return false;
                        }
                        else
                        {
                            Debug.Assert( op.State == SUCCESS );

                            // we can presume that the operation was dequeued and end
                            ConcurrencyTestingApi.TraceEvent( "Other thread finished the operation, return true." );

                            return true;
                        }
                    }
                }
            }
            else
            {
                Debug.Assert( op.State == SUCCESS );

                // we can presume that the operation was dequeued and end
                ConcurrencyTestingApi.TraceEvent( "Other thread finished the operation, return true." );

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

        if ( cancellationToken != CancellationToken.None )
        {
            throw new InvalidOperationException( "Support for cancellation tokens is not currently implemented." );
        }

        if ( timeout == TimeSpan.Zero )
        {
            // this is a peek operation - we should finish immediately ( state machine will check IsCompleted before calling *OnCompleted )

            // we need to just peek if the handle is signaled (and consume the signal in case of auto reset event)
            // this does not race with Set/Reset as we do not work with the queue
            if ( this._resetMode == (int) EventResetMode.AutoReset )
            {
                if ( SIGNALED == Interlocked.CompareExchange( ref this.SignalState, NOT_SIGNALED, SIGNALED ) )
                {
                    ConcurrencyTestingApi.TraceEvent( "Signal consumed, return awaiter with ImmediateResult=true." );

                    return new Awaiter( this, true );
                }
                else
                {
                    ConcurrencyTestingApi.TraceEvent( "Signal not consumed, return awaiter with ImmediateResult=false." );

                    return new Awaiter( this, false );
                }
            }
            else
            {
                if ( this.SignalState == SIGNALED )
                {
                    ConcurrencyTestingApi.TraceEvent( "Signal observed, return awaiter with ImmediateResult=true." );

                    return new Awaiter( this, true );
                }
                else
                {
                    ConcurrencyTestingApi.TraceEvent( "Signal observed, return awaiter with ImmediateResult=false." );

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
                    ConcurrencyTestingApi.TraceEvent( "Signal consumed, return awaiter with ImmediateResult=true." );

                    return new Awaiter( this, true );
                }
            }
            else
            {
                // MANUAL RESET:
                if ( this.SignalState == SIGNALED )
                {
                    ConcurrencyTestingApi.TraceEvent( "Signal observed, return true." );

                    return new Awaiter( this, true );
                }
            }

            // in case of both modes, if we could not end immediately, we need to return an awaiter and force the consumer to schedule a continuation
            // this awaiter will tell the state machine that the operation is not completed, forcing it to schedule a continuation
            var op =
                new WaitOperationAsync { State = CREATED, Timeout = timeout, CancellationToken = cancellationToken };

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

        if ( cancellationToken != CancellationToken.None )
        {
            throw new InvalidOperationException( "Support for cancellation tokens is not currently implemented." );
        }

        if ( timeout == TimeSpan.Zero )
        {
            // this is a peek operation - we should finish immediately ( state machine will check IsCompleted before calling *OnCompleted )

            // we need to just peek if the handle is signaled (and consume the signal in case of auto reset event)
            // this does not race with Set/Reset as we do not work with the queue
            if ( this._resetMode == (int) EventResetMode.AutoReset )
            {
                if ( SIGNALED == Interlocked.CompareExchange( ref this.SignalState, NOT_SIGNALED, SIGNALED ) )
                {
                    ConcurrencyTestingApi.TraceEvent( "Signal consumed, return awaiter with ImmediateResult=true." );

                    return new Awaiter<TData>( this, true );
                }
                else
                {
                    ConcurrencyTestingApi.TraceEvent( "Signal not consumed, return awaiter with ImmediateResult=false." );

                    return new Awaiter<TData>( this, false );
                }
            }
            else
            {
                if ( this.SignalState == SIGNALED )
                {
                    ConcurrencyTestingApi.TraceEvent( "Signal observed, return awaiter with ImmediateResult=true." );

                    return new Awaiter<TData>( this, true );
                }
                else
                {
                    ConcurrencyTestingApi.TraceEvent( "Signal observed, return awaiter with ImmediateResult=false." );

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
                    ConcurrencyTestingApi.TraceEvent( "Signal consumed, return awaiter with ImmediateResult=true." );

                    return new Awaiter<TData>( this, true );
                }
            }
            else
            {
                // MANUAL RESET:
                if ( this.SignalState == SIGNALED )
                {
                    ConcurrencyTestingApi.TraceEvent( "Signal observed, return true." );

                    return new Awaiter<TData>( this, true );
                }
            }

            // in case of both modes, if we could not end immediately, we need to return an awaiter and force the consumer to schedule a continuation
            // this awaiter will tell the state machine that the operation is not completed, forcing it to schedule a continuation
            WaitOperationAsync<TData> op =
                new() { State = CREATED, Timeout = timeout, CancellationToken = cancellationToken };

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

        ConcurrencyTestingApi.TraceEvent( "Enqueued operation." );

        if ( this._resetMode == (int) EventResetMode.AutoReset )
        {
            if ( SIGNALED == Interlocked.CompareExchange( ref this.SignalState, NOT_SIGNALED, SIGNALED ) )
            {
                ConcurrencyTestingApi.TraceEvent( "Signal taken, try to finish current operation." );

                if ( CREATED != Interlocked.CompareExchange( ref op.State, SUCCESS, CREATED ) )
                {
                    ConcurrencyTestingApi.TraceEvent( "Other thread finished the operation, use Set() to put signal back correctly." );
                    this.Set();
                }

                ConcurrencyTestingApi.TraceEvent( "Operation succeeded, activate and exit." );
                op.Activate();
            }
            else
            {
                ConcurrencyTestingApi.TraceEvent( "Event is not signaled, begin to wait." );

                // try to announce that we are going to wait (other threads need to signal the event to get us going)
                if ( CREATED == Interlocked.CompareExchange( ref op.State, WAITING, CREATED ) )
                {
                    ConcurrencyTestingApi.TraceEvent( "Operation moved to waiting state, try to consume the signal again." );

                    if ( SIGNALED == Interlocked.CompareExchange( ref this.SignalState, NOT_SIGNALED, SIGNALED ) )
                    {
                        ConcurrencyTestingApi.TraceEvent( "Signal taken, try to finish current operation." );

                        if ( WAITING == Interlocked.CompareExchange( ref op.State, SUCCESS, WAITING ) )
                        {
                            ConcurrencyTestingApi.TraceEvent( "Operation succeeded, activate and exit." );
                            op.Activate();
                        }
                        else
                        {
                            ConcurrencyTestingApi.TraceEvent( "Other thread finished the operation, use Set() to put signal back correctly." );
                            this.Set();
                        }
                    }
                    else
                    {
                        ConcurrencyTestingApi.TraceEvent( "Signal not taken, wait." );

                        if ( op.Timeout == _infiniteTimeSpan )
                        {
                            // if there is no cancellation token, we simply exit

                            if ( op.CancellationToken != CancellationToken.None )
                            {
                                throw new NotImplementedException( "Cancellation tokens are currently is not implemented." );
                            }
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
                    ConcurrencyTestingApi.TraceEvent( "Other thread finished the operation, activate operation and exit." );
                    op.Activate();
                }
            }
        }
        else
        {
            if ( this.SignalState == SIGNALED )
            {
                ConcurrencyTestingApi.TraceEvent( "Event is signaled, try to finish the operation." );

                if ( CREATED == Interlocked.CompareExchange( ref op.State, SUCCESS, CREATED ) )
                {
                    ConcurrencyTestingApi.TraceEvent( "Finished the operation, activate and exit." );
                    op.Activate();
                }
                else
                {
                    ConcurrencyTestingApi.TraceEvent( "Other thread finished the operation, exit." );
                }
            }
            else
            {
                ConcurrencyTestingApi.TraceEvent( "Event is not signaled, begin to wait." );

                if ( CREATED == Interlocked.CompareExchange( ref op.State, WAITING, CREATED ) )
                {
                    ConcurrencyTestingApi.TraceEvent( "Operation moved to waiting state, check the signal again." );

                    if ( this.SignalState == SIGNALED )
                    {
                        ConcurrencyTestingApi.TraceEvent( "Event is signaled, try to finish the operation." );

                        if ( CREATED == Interlocked.CompareExchange( ref op.State, SUCCESS, CREATED ) )
                        {
                            ConcurrencyTestingApi.TraceEvent( "Finished the operation, activate and exit." );
                            op.Activate();
                        }
                        else
                        {
                            ConcurrencyTestingApi.TraceEvent( "Other thread finished the operation, exit." );
                        }
                    }
                    else
                    {
                        ConcurrencyTestingApi.TraceEvent( "Signal not taken, wait." );

                        if ( op.Timeout == _infiniteTimeSpan )
                        {
                            // if there is no cancellation token, we simply exit

                            if ( op.CancellationToken != CancellationToken.None )
                            {
                                throw new NotImplementedException( "Cancellation tokens are currently not implemented." );
                            }
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
                    ConcurrencyTestingApi.TraceEvent( "Other thread finished the operation, activate operation and exit." );
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

        public abstract bool Activate();
    }

    internal class WaitOperationSync : WaitOperationBase
    {
        // synchronization of blocking wait
        public volatile ManualResetEventSlim Event;

        public override bool Activate()
        {
            this.Event.Set();

            return true;
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
    }

    internal sealed class WaitOperationAsync : WaitOperationAsyncBase
    {
        // caching delegate
        private static readonly WaitCallback _runContinuationWaitCallback = RunContinuation;

        // continuation
        public volatile Action Continuation;

        public override bool Activate()
        {
            var state = this.State;
            Debug.Assert( state != TIMEOUT );

            if ( state == SUCCESS )
            {
                ConcurrencyTestingApi.TraceEvent( "Operation already in SUCCESS state." );
            }
            else if ( state == Interlocked.CompareExchange( ref this.State, SUCCESS, state ) )
            {
                if ( state == CREATED )
                {
                    ConcurrencyTestingApi.TraceEvent( "Operation moved to SUCCESS state, it was CREATED." );
                }
                else
                {
                    ConcurrencyTestingApi.TraceEvent( "Operation moved to SUCCESS state, it was WAITING, schedule continuation." );
                }
            }
            else
            {
                return false;
            }

            // NOTE: this is how YieldAwaiter handles reactivation, but we omit SynchronizationContext.CurrentNoFlow which is internal
            // TODO: make sure that this reactivation algorithm is correct

            if ( this.TaskScheduler != TaskScheduler.Default )
            {
                Task.Factory.StartNew( this.Continuation, default, TaskCreationOptions.PreferFairness, this.TaskScheduler );
            }
            else if ( this.FlowContext )
            {
                ThreadPool.QueueUserWorkItem( _runContinuationWaitCallback, this.Continuation );
            }
            else
            {
                ThreadPool.UnsafeQueueUserWorkItem( _runContinuationWaitCallback, this.Continuation );
            }

            return true;
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

        public bool Result
        {
            get { return this.State == SUCCESS; }
        }

        public override bool Activate()
        {
            var state = this.State;
            Debug.Assert( state != TIMEOUT );

            if ( state == SUCCESS )
            {
                ConcurrencyTestingApi.TraceEvent( "Operation already in SUCCESS state." );
            }
            else if ( state == Interlocked.CompareExchange( ref this.State, SUCCESS, state ) )
            {
                if ( state == CREATED )
                {
                    ConcurrencyTestingApi.TraceEvent( "Operation moved to SUCCESS state, it was CREATED." );
                }
                else
                {
                    ConcurrencyTestingApi.TraceEvent( "Operation moved to SUCCESS state, it was WAITING, schedule continuation." );
                }
            }
            else
            {
                return false;
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
                ThreadPool.QueueUserWorkItem( _runContinuationWaitCallback, this );
            }
            else
            {
                ThreadPool.UnsafeQueueUserWorkItem( _runContinuationWaitCallback, this );
            }

            return true;
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
            this._owner.ScheduleContinuation( this._operation, continuation, true );
        }

        public void UnsafeOnCompleted( Action continuation )
        {
            this._owner.ScheduleContinuation( this._operation, continuation, false );
        }

        public bool GetResult()
        {
            return this._immediateResult ?? (this._operation.State == SUCCESS);
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
            this._owner.ScheduleContinuation( this.Operation, continuation, true );
        }

        public void UnsafeOnCompleted( Action<WaitOperationAsync<TData>> continuation )
        {
            this._owner.ScheduleContinuation( this.Operation, continuation, false );
        }

        public bool GetResult()
        {
            return this._immediateResult ?? (this.Operation.State == SUCCESS);
        }

        public Awaiter<TData> GetAwaiter()
        {
            return this;
        }

        public TData Data { get { return this.Operation.Data; } set { this.Operation.Data = value; } }
    }
}