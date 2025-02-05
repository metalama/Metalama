// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using System;

namespace Metalama.Framework.Engine.Utilities.Caching;

[PublicAPI]
public readonly struct ObjectPoolHandle<T> : IDisposable
    where T : class
{
    private readonly ObjectPool<T>? _pool;

    public T Value { get; }

    public bool IsDefault => this.Value == null;

    public ObjectPoolHandle( ObjectPool<T> pool, T value )
    {
        this._pool = pool;
        this.Value = value;
    }

    public ObjectPoolHandle( T value )
    {
        this.Value = value;
    }

    public void Dispose()
    {
        this._pool?.Free( this.Value );
    }

    public override string ToString() => this.Value.ToString()!;
}