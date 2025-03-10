// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Advising;

internal sealed class LazyObjectReader : IObjectReader
{
    private readonly Lazy<IObjectReader> _lazyUnderlying;

    public LazyObjectReader( Lazy<IObjectReader> lazyUnderlying ) 
    {
        this._lazyUnderlying = lazyUnderlying;
    }

    private IObjectReader Underlying => this._lazyUnderlying.Value;

    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => this.Underlying.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable) this.Underlying).GetEnumerator();

    public int Count => this.Underlying.Count;

    public bool ContainsKey( string key ) => this.Underlying.ContainsKey( key );

    public bool TryGetValue( string key, out object? value ) => this.Underlying.TryGetValue( key, out value );

    public object? this[ string key ] => this.Underlying[key];

    public IEnumerable<string> Keys => this.Underlying.Keys;

    public IEnumerable<object?> Values => this.Underlying.Values;

    public object? Source => this.Underlying.Source;
}