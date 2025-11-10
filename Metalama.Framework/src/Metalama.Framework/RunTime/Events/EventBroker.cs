// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Metalama.Framework.RunTime.Events;

#pragma warning disable SA1402

/// <summary>
/// Manages event handlers for event override advice when raise templates are specified.
/// Provides thread-safe operations for adding/removing handlers and event invocation.
/// </summary>
/// <typeparam name="TDelegate">The event delegate type.</typeparam>
/// <typeparam name="TArgs">The event arguments type  (i.e. the arguments of <typeparamref name="TDelegate"/> packed as a tuple).</typeparam>
/// <typeparam name="TState">An opaque state stored by the <see cref="EventBroker{TDelegate,TArgs,TState}"/> and passed along to the <see cref="IEventAdapter{TDelegate,TArgs,TState}"/>.
/// For instance events, typically set to the declaring type of the event. For static events, it is typically set to <see cref="None"/>.</typeparam> 
public sealed class EventBroker<TDelegate, TArgs, TState>
    where TDelegate : Delegate
{
    private readonly TState _state;
    private readonly IEventAdapter<TDelegate, TArgs, TState> _adapter;
    private DelegateList<TDelegate, TArgs, TState> _handlers;
    private SpinLock _lock;

    /// <summary>
    /// Gets a delegate that calls the <see cref="Invoke" /> method.
    /// </summary>
    [field: MaybeNull]
    public TDelegate InvocationDelegate => field ??= this._adapter.GetBrokerInvocationDelegate( this );

    /// <summary>
    /// Initializes a new instance of the <see cref="EventBroker{TDelegate,TOwner,TArgs}"/> class.
    /// </summary>
    /// <param name="owner">The object that owns this event broker.</param>
    /// <param name="adapter">Delegates required for operation of this class.</param>
    private EventBroker( IEventAdapter<TDelegate, TArgs, TState> adapter, TState state )
    {
        this._state = state;
        this._adapter = adapter;
    }

    /// <summary>
    /// Adds an event handler.
    /// </summary>
    /// <param name="handler">The handler to add.</param>
    public void AddHandler( TDelegate? handler )
    {
        if ( handler == null )
        {
            return;
        }

        var lockTaken = false;

        try
        {
            this._lock.TryEnter( EventBrokerServices.LockTimeout, ref lockTaken );

            if ( !lockTaken )
            {
                throw new TimeoutException( "Timeout waiting to acquire lock in ActionEventBroker. This indicates complex underlying event or a deadlock." );
            }

            var wasFirst = this._handlers.IsEmpty;
            this._handlers.Add( handler );

            if ( wasFirst )
            {
                this._adapter.AddHandler( this.InvocationDelegate, this._state );
            }
        }
        finally
        {
            if ( lockTaken )
            {
                this._lock.Exit();
            }
        }
    }

    /// <summary>
    /// Removes an event handler.
    /// </summary>
    /// <param name="handler">The handler to remove.</param>
    public void RemoveHandler( TDelegate? handler )
    {
        if ( handler == null )
        {
            return;
        }

        var lockTaken = false;

        try
        {
            this._lock.TryEnter( EventBrokerServices.LockTimeout, ref lockTaken );

            if ( !lockTaken )
            {
                throw new TimeoutException( "Timeout waiting to acquire lock in ActionEventBroker. This indicates complex underlying event or a deadlock." );
            }

            this._handlers.Remove( handler );

            if ( this._handlers.IsEmpty )
            {
                this._adapter.RemoveHandler( this.InvocationDelegate, this._state );
            }
        }
        finally
        {
            if ( lockTaken )
            {
                this._lock.Exit();
            }
        }
    }

    /// <summary>
    /// Invokes all registered event handlers. This method should be used when the delegate has <c>out</c> or <c>ref</c> parameters
    /// or has a non-void return type. In the latter case, the return value must be mapped as a tuple element.
    /// </summary>
    /// <param name="args">The event arguments.</param>
    public void InvokeByRef( ref TArgs args )
    {
        this._handlers.Invoke( this._adapter.InvokeHandler, ref args, this._state );
    }

    /// <summary>
    /// Invokes all registered event handlers. This method should be used when all delegate parameters are read-only
    /// and the delegate has a void return type.
    /// </summary>
    /// <param name="args">The event arguments.</param>
    public void Invoke( in TArgs args )
    {
        ref var writableRef = ref Unsafe.AsRef( in args );
        this.InvokeByRef( ref writableRef );
    }

    /// <summary>
    /// Implicitly converts the broker to its delegate type.
    /// </summary>
    public static implicit operator TDelegate( EventBroker<TDelegate, TArgs, TState> broker ) => broker.InvocationDelegate;

    internal static void EnsureInitializedImpl(
        ref EventBroker<TDelegate, TArgs, TState>? field,
        DelegateEventAdapter<TDelegate, TArgs, TState> adapter,
        TState state = default! )
    {
        if ( field != null )
        {
            return;
        }

        var newBroker = new EventBroker<TDelegate, TArgs, TState>( adapter, state );

        Interlocked.CompareExchange( ref field, newBroker, null );

        // Don't care about the result - if it failed, another thread won.
    }
}

/// <summary>
/// Exposes a method that initializes an <see cref="EventBroker{TDelegate,TArgs,TState}"/>.
/// </summary>
public static class EventBroker
{
    /// <summary>
    /// Thread-safely initializes an <see cref="EventBroker{TDelegate,TArgs,TState}"/> field.
    /// </summary>
    /// <param name="field">The field to initialize.</param>
    /// <param name="adapter">Delegates required for inner working of event brokers.</param>
    public static void EnsureInitialized<TDelegate, TArgs, TState>(
        ref EventBroker<TDelegate, TArgs, TState>? field,
        DelegateEventAdapter<TDelegate, TArgs, TState> adapter,
        TState state = default! )
        where TDelegate : Delegate
    {
        EventBroker<TDelegate, TArgs, TState>.EnsureInitializedImpl( ref field, adapter, state );
    }
}