// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Framework.RunTime;

/// <summary>
/// Thread-safe collection of delegates used by event brokers to manage event handlers.
/// </summary>
/// <typeparam name="TDelegate">The delegate type. Must be a reference type delegate.</typeparam>
/// <typeparam name="TArgs">The event arguments type.</typeparam>
internal class DelegateList<TDelegate, TArgs>
    where TDelegate : class, Delegate
{
    private readonly Action<TDelegate, object, TArgs> _invoker;
    private volatile TDelegate? _delegates;

    /// <summary>
    /// Gets a value indicating whether no delegates are registered.
    /// </summary>
    public bool IsEmpty => this._delegates == null;

    /// <summary>
    /// Initializes a new instance of the <see cref="DelegateList{TDelegate, TArgs}"/> class with the specified invoker.
    /// </summary>
    /// <param name="invoker">Delegate that defines how to invoke handlers.</param>
    public DelegateList( Action<TDelegate, object, TArgs> invoker )
    {
        this._invoker = invoker ?? throw new ArgumentNullException( nameof( invoker ) );
    }

    /// <summary>
    /// Adds a delegate using atomic operations.
    /// </summary>
    /// <param name="del">The delegate to add.</param>
    public void Add( TDelegate del )
    {
        if ( del == null )
        {
            throw new ArgumentNullException( nameof( del ) );
        }

        while (true)
        {
            var currentValue = this._delegates;
            var newValue = (TDelegate) Delegate.Combine( currentValue, del );

            if ( System.Threading.Interlocked.CompareExchange( ref this._delegates, newValue, currentValue ) == currentValue )
            {
                // Successfully updated the delegates.
                return;
            }
        }
    }

    /// <summary>
    /// Removes a delegate using atomic operations.
    /// </summary>
    /// <param name="del">The delegate to remove.</param>
    public void Remove( TDelegate del )
    {
        if ( del == null )
        {
            throw new ArgumentNullException( nameof( del ) );
        }

        while ( true )
        {
            var currentValue = this._delegates;
            var newValue = (TDelegate?) Delegate.Remove( currentValue, del );

            if ( System.Threading.Interlocked.CompareExchange( ref this._delegates, newValue, currentValue ) == currentValue )
            {
                // Successfully updated the delegates.
                return;
            }
        }
    }
    
    /// <summary>
    /// Invokes all registered delegates using the configured invoker.
    /// </summary>
    /// <param name="me">The event owner object.</param>
    /// <param name="args">The event arguments.</param>
    public void Invoke( object me, TArgs args )
    {
        var currentValue = this._delegates;

        if (currentValue == null)
        {
            // No delegates to invoke.
            return;
        }

        foreach (var @delegate in currentValue.GetInvocationList())
        {
            this._invoker.Invoke( (TDelegate)@delegate, me, args );
        }
    }
}