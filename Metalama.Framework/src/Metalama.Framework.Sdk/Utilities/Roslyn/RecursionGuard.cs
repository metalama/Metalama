// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Utilities.Roslyn;

/// <summary>
/// Protects a recursive syntax visitor against the exhaustion of the stack by moving the processing of a subtree to a thread
/// of the thread pool when the stack of the current thread is close to its limit.
/// </summary>
/// <remarks>
/// <para>
/// Roslyn calls <see cref="RuntimeHelpers.EnsureSufficientExecutionStack" /> in its syntax visitors. This method throws an
/// <see cref="InsufficientExecutionStackException"/> when the remaining stack is below a margin that depends on the runtime.
/// The guard must switch to another thread before this happens, and while some stack is still available, because the code
/// that runs between two calls of <see cref="IncrementDepth"/>, including calls to the semantic model, can itself use a
/// significant amount of stack.
/// </para>
/// <para>
/// When <see cref="StackLimits"/> can determine the bounds of the stack of the current thread, the guard switches when
/// the stack available before <see cref="RuntimeHelpers.EnsureSufficientExecutionStack" /> fails is smaller than
/// <see cref="_minAvailableStackSize"/>. This makes the guard independent of the
/// stack size of the threads that it runs on, and of the amount of stack that the caller has already used. When the bounds
/// are not known, the guard switches every <see cref="_fallbackSwitchInterval"/> levels of recursion.
/// </para>
/// <para>
/// Roslyn does not support an unlimited recursion depth, and supporting it here would hide bugs that cause an infinite
/// recursion, so the number of nested switches is limited.
/// </para>
/// <para>
/// This is a mutable struct, so it must not be stored in a <see langword="readonly" /> field.
/// </para>
/// </remarks>
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

    /// <summary>
    /// The number of nested switches that are active on the current call stack.
    /// </summary>
    private int _switchCount;

    /// <summary>
    /// The available stack size, in bytes, below which the guard switches to another thread when the bounds of the stack
    /// are known. The available stack size is measured by <see cref="StackLimits.TryGetAvailableStackSize"/>.
    /// </summary>
    private const long _minAvailableStackSize = 128 * 1024;

    /// <summary>
    /// The maximum number of nested switches when the bounds of the stack are known.
    /// </summary>
    private const int _maxSwitches = 8;

    /// <summary>
    /// The number of levels of recursion between two switches when the bounds of the stack are not known.
    /// </summary>
    /// <remarks>
    /// An <see cref="InsufficientExecutionStackException"/> was observed in <see cref="SafeSyntaxWalker"/> at a depth of
    /// 750, so this value is smaller than that.
    /// </remarks>
    private const int _fallbackSwitchInterval = 500;

    /// <summary>
    /// The maximum number of switches when the bounds of the stack are not known.
    /// </summary>
    private const int _fallbackMaxSwitches = 6;

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

    /// <summary>
    /// Gets a value indicating whether the processing of the current node must be moved to another thread by calling
    /// one of the <c>Switch</c> methods.
    /// </summary>
    public readonly bool ShouldSwitch
    {
        get
        {
            if ( StackLimits.TryGetAvailableStackSize( out var availableStackSize ) )
            {
                return availableStackSize < _minAvailableStackSize && this._switchCount < _maxSwitches;
            }
            else
            {
                return this._recursionDepth % _fallbackSwitchInterval == 0 && this._recursionDepth <= _fallbackSwitchInterval * _fallbackMaxSwitches;
            }
        }
    }

    public void Switch<TState>( TState state, Action<TState> recursiveAction )
    {
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits

        // The ContinueWith is used to prevent inline execution of the Task. It also rethrows the exception of the Task,
        // because GetResult is called on the continuation and not on the Task itself.

#if DEBUG
        var threadStack = this._threadIdStack ??= new ConcurrentStack<int>();
#endif

        this._switchCount++;

        try
        {
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
                .ContinueWith( task => task.GetAwaiter().GetResult(), TaskScheduler.Default )
                .GetAwaiter()
                .GetResult();
        }
        finally
        {
            this._switchCount--;
        }
#pragma warning restore VSTHRD002
    }

    public TResult Switch<TState, TResult>( TState state, Func<TState, TResult> recursiveFunction )
    {
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits

        // The ContinueWith is used to prevent inline execution of the Task. It also rethrows the exception of the Task,
        // because GetResult is called on the continuation and not on the Task itself.

#if DEBUG
        var threadStack = this._threadIdStack ??= new ConcurrentStack<int>();
#endif

        this._switchCount++;

        try
        {
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
        }
        finally
        {
            this._switchCount--;
        }
#pragma warning restore VSTHRD002
    }

    public void Failed()
    {
#if DEBUG
        this._failed = true;
#endif
    }
}