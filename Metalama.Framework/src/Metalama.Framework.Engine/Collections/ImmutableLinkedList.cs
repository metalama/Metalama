// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Collections;

[PublicAPI]
public sealed class ImmutableLinkedList<T> : IReadOnlyCollection<T>
{
    private readonly T _value;
    private readonly ImmutableLinkedList<T>? _next;

    public static ImmutableLinkedList<T> Empty { get; } = new( default!, null );

    private ImmutableLinkedList( T value, ImmutableLinkedList<T>? next )
    {
        this._value = value;
        this._next = next;

        if ( next != null )
        {
            this.Count = next.Count + 1;
        }
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => this.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

    public Enumerator GetEnumerator() => new( this );

    public int Count { get; }

    public ImmutableLinkedList<T> Insert( T value ) => new( value, this );

    public bool Contains( T value, IEqualityComparer<T> comparer )
    {
        for ( var i = this; i is { Count: > 0 }; i = i._next )
        {
            if ( comparer.Equals( i._value, value ) )
            {
                return true;
            }
        }

        return false;
    }

    public struct Enumerator : IEnumerator<T>
    {
        private ImmutableLinkedList<T>? _currentNode;
        private ImmutableLinkedList<T>? _nextNode;

        public Enumerator( ImmutableLinkedList<T>? nextNode ) : this()
        {
            this._nextNode = nextNode;
        }

        public bool MoveNext()
        {
            if ( this._nextNode == null || this._nextNode.Count == 0 )
            {
                return false;
            }

            this._currentNode = this._nextNode;
            this._nextNode = this._nextNode._next;

            return true;
        }

        public void Reset() => throw new NotSupportedException();

        public readonly T Current => this._currentNode != null ? this._currentNode._value : throw new InvalidOperationException();

        readonly object? IEnumerator.Current => this.Current;

        public void Dispose() { }
    }
}