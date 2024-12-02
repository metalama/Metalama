// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;

namespace Metalama.Framework.Engine.Utilities;

public readonly struct DisposeAction : IDisposable
{
    private readonly Action? _action;

    internal DisposeAction( Action action )
    {
        this._action = action;
    }

    public void Dispose() => this._action?.Invoke();
}