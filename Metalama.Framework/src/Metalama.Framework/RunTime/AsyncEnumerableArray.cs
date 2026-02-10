// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if NET5_0_OR_GREATER

using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

namespace Metalama.Framework.RunTime;

/// <summary>
/// Wraps an <see cref="ImmutableArray{T}"/> as an <see cref="IAsyncEnumerable{T}"/>.
/// </summary>
/// <typeparam name="T"></typeparam>
/// <remarks>This class is only used by Metalama to represent an empty <see cref="IAsyncEnumerable{T}"/> through the <see cref="Empty"/> property.</remarks>
[PublicAPI]
public sealed class AsyncEnumerableArray<T> : IReadOnlyList<T>, IAsyncEnumerable<T>
{
    public static AsyncEnumerableArray<T> Empty { get; } = new AsyncEnumerableArray<T>( Array.Empty<T>() );
    
    private readonly T[] _array;

    public AsyncEnumerableArray( T[] array )
    {
        this._array = array;
    }

    public IEnumerator<T> GetEnumerator()
    {
        foreach ( var item in this._array )
        {
            yield return item;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

    public int Count => this._array.Length;

    public T this[ int index ]
    {
        get => this._array[index];
        set => this._array[index] = value;
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public async IAsyncEnumerator<T> GetAsyncEnumerator( CancellationToken cancellationToken = default )
    {
        foreach ( var item in this._array )
        {
            cancellationToken.ThrowIfCancellationRequested();

            yield return item;
        }
    }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}

#endif