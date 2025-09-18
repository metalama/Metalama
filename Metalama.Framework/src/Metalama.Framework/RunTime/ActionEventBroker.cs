// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Threading;

namespace Metalama.Framework.RunTime;

/// <summary>
/// Manages event handlers for event override advice when raise templates are specified.
/// Provides thread-safe operations for adding/removing handlers and event invocation.
/// </summary>
/// <typeparam name="TDelegate">The event delegate type.</typeparam>
/// <typeparam name="TArgs">The event arguments type.</typeparam>
public sealed class ActionEventBroker<TDelegate, TArgs>
    where TDelegate : Delegate
{
    private readonly DelegateList<TDelegate, TArgs> _handlers;
    private readonly object _owner;
    private readonly ActionEventBrokerDelegateSet<TDelegate, TArgs> _delegates;
    private TDelegate? _invocationDelegate;

    /// <summary>
    /// Gets a delegate that calls the <see cref="Invoke" /> method.
    /// </summary>
    public TDelegate InvocationDelegate => this._invocationDelegate ??= this._delegates.ToDelegate( this );

    /// <summary>
    /// Initializes a new instance of the <see cref="ActionEventBroker{TDelegate, TArgs}"/> class.
    /// </summary>
    /// <param name="owner">The object that owns this event broker.</param>
    /// <param name="delegates">Delegates required for operation of this class.</param>
    private ActionEventBroker(
        object owner,
        ActionEventBrokerDelegateSet<TDelegate, TArgs> delegates)
    {
        this._owner = owner;
        this._delegates = delegates;
        this._handlers = new();
    }

    /// <summary>
    /// Adds an event handler.
    /// </summary>
    /// <param name="handler">The handler to add.</param>
    public void AddHandler( TDelegate? handler )
    {
        if ( handler != null )
        {
            var lockTaken = false;

            try
            {
                Monitor.TryEnter( this._handlers, EventBrokerServices.LockTimeout, ref lockTaken );

                if ( !lockTaken )
                {
                    throw new TimeoutException( "Timeout waiting to acquire lock in ActionEventBroker. This indicates complex underlying event or a deadlock." );
                }

                var wasFirst = this._handlers.IsEmpty;
                this._handlers.Add( handler );

                if ( wasFirst )
                {
                    this._delegates.BaseAdd( this.InvocationDelegate, this._owner );
                }
            }
            finally
            {
                if ( lockTaken )
                {
                    Monitor.Exit( this._handlers );
                }
            }
        }
    }

    /// <summary>
    /// Removes an event handler.
    /// </summary>
    /// <param name="handler">The handler to remove.</param>
    public void RemoveHandler( TDelegate? handler )
    {
        if ( handler != null )
        {
            var lockTaken = false;

            try
            {
                Monitor.TryEnter( this._handlers, EventBrokerServices.LockTimeout, ref lockTaken );

                if ( !lockTaken )
                {
                    throw new TimeoutException( "Timeout waiting to acquire lock in ActionEventBroker. This indicates complex underlying event or a deadlock." );
                }

                this._handlers.Remove( handler );

                if ( this._handlers.IsEmpty )
                {
                    this._delegates.BaseRemove( this.InvocationDelegate, this._owner );
                }
            }
            finally
            {
                if ( lockTaken )
                {
                    Monitor.Exit( this._handlers );
                }
            }
        }
    }

    /// <summary>
    /// Invokes all registered event handlers.
    /// </summary>
    /// <param name="args">The event arguments.</param>
    public void Invoke( TArgs args )
    {
        this._handlers.Invoke( this._delegates.Invoker, this._owner, args );
    }

    /// <summary>
    /// Implicitly converts the broker to its delegate type.
    /// </summary>
    public static implicit operator TDelegate( ActionEventBroker<TDelegate, TArgs> broker )
    {
        return broker.InvocationDelegate;
    }

    /// <summary>
    /// Thread-safely initializes an event broker field.
    /// </summary>
    /// <param name="field">The field to initialize.</param>
    /// <param name="owner">The event owner object.</param>
    /// <param name="delegates">Delegates required for inner working of event brokers.</param>
    public static void EnsureInitialized( ref ActionEventBroker<TDelegate, TArgs>? field, object owner, ActionEventBrokerDelegateSet<TDelegate, TArgs> delegates )
    {
        if ( field != null )
        {
            return;
        }

        var newBroker = new ActionEventBroker<TDelegate, TArgs>( owner, delegates );

        Interlocked.CompareExchange( ref field, newBroker, null );

        // Don't care about the result - if it failed, another thread won.
    }
}