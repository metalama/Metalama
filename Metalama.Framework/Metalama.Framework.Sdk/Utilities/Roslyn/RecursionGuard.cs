// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Utilities.Roslyn;

/// <summary>
/// <para>If there is a risk of running out of stack space on the current thread, switches to a different thread.
/// Note that <see cref="RuntimeHelpers.EnsureSufficientExecutionStack" />  wouldn't work here, since it doesn't take into account executing potentially
/// deeply recursive Roslyn methods.</para>
/// <para>Roslyn does not support unlimited recursion depth and supporting it here would mask infinite recursion bugs,
/// so the switching is limited to just a small number of tasks.</para>
/// <para>Note that this is a mutable struct, so make sure not to store it in a <see langword="readonly" /> field.</para>
/// </summary>
internal struct RecursionGuard
{
#if DEBUG
    private readonly int _threadId;
    private readonly object _owner;
#endif

    private int _recursionDepth;

#if DEBUG
    private ConcurrentStack<int>? _threadIdStack;
    private volatile bool _failed;
#endif

    // InsufficientExecutionStackException can be observed in SafeSyntaxWalker when this is 750, so set it to a value that is smaller than that.
    private const int _maxRecursionDepth = 500;
    private const int _maxTasks = 6;

    public RecursionGuard( object owner )
    {
#if DEBUG
        this._threadId = Thread.CurrentThread.ManagedThreadId;
        this._owner = owner;
#endif
    }

    public void IncrementDepth()
    {
#if DEBUG
        if ( this._failed )
        {
            throw new InvalidOperationException( $"Object ({this._owner.GetType().FullName}) is being used after a failure occured." );
        }

        if ( this._threadIdStack == null || this._threadIdStack.Count == 0 )
        {
            if ( this._threadId != Thread.CurrentThread.ManagedThreadId )
            {
                throw new InvalidOperationException( $"Object ({this._owner.GetType().FullName}) is being used across threads." );
            }
        }
        else
        {
            this._threadIdStack.TryPeek( out var currentThreadId );

            if ( currentThreadId != Thread.CurrentThread.ManagedThreadId )
            {
                throw new InvalidOperationException( $"Object ({this._owner.GetType().FullName}) is being used across threads." );
            }
        }
#endif

        this._recursionDepth++;
    }

    public void DecrementDepth() => this._recursionDepth--;

    public readonly bool ShouldSwitch => this._recursionDepth % _maxRecursionDepth == 0 && this._recursionDepth <= _maxRecursionDepth * _maxTasks;

    public void Switch<TState>( TState state, Action<TState> recursiveAction )
    {
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits

        // The ContinueWith is used to prevent inline execution of the Task.

#if DEBUG
        var threadStack = this._threadIdStack ??= new ConcurrentStack<int>();
#endif

        Task.Run(
                () =>
                {
                    try
                    {
#if DEBUG
                        threadStack.Push( Thread.CurrentThread.ManagedThreadId );
#endif
                        recursiveAction( state );
                    }
                    finally
                    {
#if DEBUG
                        threadStack.TryPop( out _ );
#endif
                    }
                } )
            .ContinueWith( _ => { }, TaskScheduler.Default )
            .GetAwaiter()
            .GetResult();
#pragma warning restore VSTHRD002
    }

    public TResult Switch<TState, TResult>( TState state, Func<TState, TResult> recursiveFunction )
    {
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits

        // The ContinueWith is used to prevent inline execution of the Task.

#if DEBUG
        var threadStack = this._threadIdStack ??= new ConcurrentStack<int>();
#endif

        return
            Task.Run(
                    () =>
                    {
                        try
                        {
#if DEBUG
                            threadStack.Push( Thread.CurrentThread.ManagedThreadId );
#endif
                            return recursiveFunction( state );
                        }
                        finally
                        {
#if DEBUG
                            threadStack.TryPop( out _ );
#endif
                        }
                    } )
                .ContinueWith( task => task.GetAwaiter().GetResult(), TaskScheduler.Default )
                .GetAwaiter()
                .GetResult();
#pragma warning restore VSTHRD002
    }

    public void Failed()
    {
#if DEBUG
        this._failed = true;
#endif
    }
}