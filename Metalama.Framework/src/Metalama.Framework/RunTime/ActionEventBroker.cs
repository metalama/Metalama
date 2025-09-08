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
public class ActionEventBroker<TDelegate, TArgs>
    where TDelegate : Delegate
{
    private readonly DelegateList<TDelegate, TArgs> _handlers;
    private readonly object _owner;
    private readonly Func<ActionEventBroker<TDelegate, TArgs>, TDelegate> _toDelegate;
    private TDelegate? _invocationDelegate;

    /// <summary>
    /// Gets a delegate that calls the <see cref="Invoke" /> method.
    /// </summary>
    public TDelegate InvocationDelegate => this._invocationDelegate ??= this._toDelegate( this );

    /// <summary>
    /// Initializes a new instance of the <see cref="ActionEventBroker{TDelegate, TArgs}"/> class.
    /// </summary>
    /// <param name="owner">The object that owns this event broker.</param>
    /// <param name="invoker">Delegate that defines how to invoke handlers.</param>
    /// <param name="toDelegate">Delegate that converts this broker to the target delegate type.</param>
    private ActionEventBroker(
        object owner,
        Action<TDelegate, object, TArgs> invoker, /* Static delegate */
        Func<ActionEventBroker<TDelegate, TArgs>, TDelegate> toDelegate /* Static delegate */ )
    {
        this._owner = owner;
        this._toDelegate = toDelegate;
        this._handlers = new DelegateList<TDelegate, TArgs>( invoker );
    }

    /// <summary>
    /// Adds an event handler.
    /// </summary>
    /// <param name="handler">The handler to add.</param>
    /// <returns><c>true</c> if this was the first handler added; otherwise, <c>false</c>.</returns>
    public bool AddHandler( TDelegate? handler )
    {
        if ( handler != null )
        {
            var isFirst = this._handlers.IsEmpty;
            this._handlers.Add( handler );
            return isFirst;
        }

        return true;
    }

    /// <summary>
    /// Removes an event handler.
    /// </summary>
    /// <param name="handler">The handler to remove.</param>
    /// <returns><c>true</c> if no handlers remain after removal; otherwise, <c>false</c>.</returns>
    public bool RemoveHandler( TDelegate? handler )
    {
        if ( handler != null )
        {
            this._handlers.Remove( handler );
        }

        return this._handlers.IsEmpty;
    }

    /// <summary>
    /// Invokes all registered event handlers.
    /// </summary>
    /// <param name="args">The event arguments.</param>
    public void Invoke( TArgs args )
    {
        this._handlers.Invoke( this._owner, args );
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
    /// <param name="invokerDelegate">Delegate that defines how to invoke handlers.</param>
    /// <param name="castDelegate">Delegate that converts the broker to the target delegate type.</param>
    public static void Initialize( ref ActionEventBroker<TDelegate, TArgs>? field, object owner, Action<TDelegate, object, TArgs> invokerDelegate, Func<ActionEventBroker<TDelegate, TArgs>, TDelegate> castDelegate )
    {
        if ( field != null )
        {
            return;
        }

        var newBroker = new ActionEventBroker<TDelegate, TArgs>( owner, invokerDelegate, castDelegate );

        Interlocked.CompareExchange( ref field, newBroker, null );

        // Don't care about the result - if it failed, another thread won.
    }
}