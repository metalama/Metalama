// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;

namespace Metalama.Framework.Workspaces;

internal sealed class Future<T>
{
    private T _value = default!;
    private bool _isSet;

    public T Value
    {
        get
        {
            if ( !this._isSet )
            {
                throw new InvalidOperationException();
            }

            return this._value;
        }
        set
        {
            if ( this._isSet )
            {
                throw new InvalidOperationException();
            }

            this._isSet = true;
            this._value = value;
        }
    }
}