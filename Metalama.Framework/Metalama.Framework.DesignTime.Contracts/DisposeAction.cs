// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;

namespace Metalama.Framework.DesignTime.Contracts;

internal sealed class DisposeAction : IDisposable
{
    private readonly Action _action;

    public DisposeAction( Action action )
    {
        this._action = action;
    }

    public void Dispose()
    {
        this._action();
    }
}