// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Metalama.Framework.Engine.Collections;

internal sealed class ConcurrentLinkedList<T> : IReadOnlyCollection<T>
{
    private volatile Node? _head;
    private int _count;

    public void Add( T item )
    {
        while ( true )
        {
            var head = this._head;
            var node = new Node( item, head );

            if ( Interlocked.CompareExchange( ref this._head, node, head ) == head )
            {
                // Make sure that we increment the count after the node has been inserted, so the Count property is never higher than the actual
                // number of nodes.
                Interlocked.Increment( ref this._count );

                return;
            }
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        for ( var node = this._head; node != null; node = node.Next )
        {
            yield return node.Value;
        }
    }

    public List<T> ToList()
    {
        var count = this._count;
        var list = new List<T>( count );

        var node = this._head;

        for ( var i = 0; i < count; i++ )
        {
            list.Add( node!.Value );
            node = node.Next;
        }

        return list;
    }

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

    private sealed class Node
    {
        public Node( T value, Node? next )
        {
            this.Value = value;
            this.Next = next;
        }

        public T Value { get; }

        public Node? Next { get; }
    }

    public int Count => this._count;
}