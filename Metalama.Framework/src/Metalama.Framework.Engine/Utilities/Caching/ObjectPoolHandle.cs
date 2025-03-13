// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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