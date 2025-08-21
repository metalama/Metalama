// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using System.Text;

namespace Metalama.Framework.RunTime;

internal struct DelegateList<TDelegate, TArgs>
    where TDelegate : class, Delegate
{
    private readonly Action<TDelegate, object, TArgs> _invoker;
    private volatile TDelegate? _delegates;

    public bool IsEmpty => this._delegates == null;

    public DelegateList( Action<TDelegate, object, TArgs> invoker )
    {
        this._invoker = invoker ?? throw new ArgumentNullException( nameof( invoker ) );
    }

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
    public void Invoke( object me, TArgs args )
    {
        var currentValue = this._delegates;

        if (currentValue == null)
        {
            // No delegates to invoke.
            return;
        }

        this._invoker.Invoke( currentValue, me, args );
    }
}
