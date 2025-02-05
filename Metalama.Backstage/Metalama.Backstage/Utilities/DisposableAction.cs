// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;

namespace Metalama.Backstage.Utilities;

public readonly struct DisposableAction : IDisposable
{
    private readonly Action? _action;

    public DisposableAction( Action? action )
    {
        this._action = action;
    }

    public void Dispose()
    {
        this._action?.Invoke();
    }
}