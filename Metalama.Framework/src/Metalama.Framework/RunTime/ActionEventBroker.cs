// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Threading;

namespace Metalama.Framework.RunTime;

public class ActionEventBroker<TDelegate, TArgs>
where TDelegate : Delegate
{
    private readonly DelegateList<TDelegate, TArgs> _handlers;
    private readonly object _owner;
    private readonly Func<ActionEventBroker<TDelegate, TArgs>, TDelegate> _toDelegate;
    private TDelegate? _invocationDelegate;

    /// <summary>
    ///     Gets a delegate that calls the <see cref="Invoke" /> method.
    /// </summary>
    public TDelegate InvocationDelegate => this._invocationDelegate ??= this._toDelegate( this );
    
    public ActionEventBroker(
        object owner,
        Action<TDelegate, object, TArgs> invoker, /* Static delegate */
        Func<ActionEventBroker<TDelegate, TArgs>, TDelegate> toDelegate /* Static delegate */ )
    {
        this._owner = owner;
        this._toDelegate = toDelegate;
        this._handlers = new DelegateList<TDelegate, TArgs>( invoker );
    }

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

    public bool RemoveHandler( TDelegate? handler )
    {
        if ( handler != null )
        {
            this._handlers.Remove( handler );
        }

        return this._handlers.IsEmpty;
    }

    public void Invoke( TArgs args )
    {
        this._handlers.Invoke( this._owner, args );
    }

    public static implicit operator TDelegate( ActionEventBroker<TDelegate, TArgs> broker )
    {
        return broker.InvocationDelegate;
    }

    public static void InitializeField( ref ActionEventBroker<TDelegate, TArgs>? field, object owner, Action<TDelegate, object, TArgs> invokerDelegate, Func<ActionEventBroker<TDelegate, TArgs>, TDelegate> castDelegate )
    {
        var newBroker = new ActionEventBroker<TDelegate, TArgs>( owner, invokerDelegate, castDelegate );

        Interlocked.CompareExchange( ref field, newBroker, null );

        // Don't care about the result - if it failed, another thread won.
    }
}