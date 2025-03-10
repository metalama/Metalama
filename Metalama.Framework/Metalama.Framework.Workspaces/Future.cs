// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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