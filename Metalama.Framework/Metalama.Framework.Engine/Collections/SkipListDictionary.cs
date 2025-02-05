// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace Metalama.Framework.Engine.Collections
{
    /// <summary>
    /// Represents a SkipList.  A SkipList is a combination of a BST and a sorted link list, providing
    /// sub-linear access, insert, and deletion running times.  It is a randomized data structure, randomly
    /// choosing the heights of the nodes in the SkipList.
    /// </summary>
    /// <typeparam name="TKey">Type of elements contained within the SkipList.</typeparam>
    /// <typeparam name="TValue">Type of values.</typeparam>
    internal sealed class SkipListDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IList<KeyValuePair<TKey, TValue>>
    {
        private const double _prob = 0.5; // the probability used in determining the heights of the SkipListNodes
        private readonly Random _random;
        private readonly IComparer<TKey> _comparer;
        private Node _head; // a reference to the head of the SkipList
        private ValueCollection? _valueCollection;

        public SkipListDictionary( IComparer<TKey>? comparer = null, int? seed = null )
        {
            this._head = Node.CreateHead();
            this._random = seed == null ? new Random() : new Random( seed.Value );

            this._comparer = comparer ?? Comparer<TKey>.Default;
        }

        /// <summary>
        /// Selects a height for a new SkipListNode using the "loaded dice" technique.
        /// The value selected is between 1 and maxLevel.
        /// </summary>
        /// <param name="maxLevel">The maximum value ChooseRandomHeight can return.</param>
        /// <returns>A randomly chosen integer value between 1 and maxLevel.</returns>
        private int ChooseRandomHeight( int maxLevel )
        {
            var level = 1;

            while ( this._random.NextDouble() < _prob && level < maxLevel )
            {
                level++;
            }

            return level;
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Add( KeyValuePair<TKey, TValue> item ) => this.Add( item.Key, item.Value );

        // Clears out the contents of the SkipList and creates a new head, with height 1.
        public void Clear()
        {
            // create a new head
            this._head = Node.CreateHead();
            this.Count = 0;
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains( KeyValuePair<TKey, TValue> item ) => throw new NotImplementedException();

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo( KeyValuePair<TKey, TValue>[] array, int arrayIndex )
        {
            var i = 0;

            foreach ( var item in this )
            {
                array[i] = item;
                i++;
            }
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove( KeyValuePair<TKey, TValue> item ) => this.Remove( item.Key );

        /// <summary>
        /// Determines if a particular element is contained within the SkipList.
        /// </summary>
        /// <param name="value">The value to search for.</param>
        /// <returns>True if value is found in the SkipList; false otherwise.</returns>
        public bool ContainsKey( TKey value )
        {
            var current = this._head;

            // first, determine the nodes that need to be updated at each level
            for ( var i = this._head.Height - 1; i >= 0; i-- )
            {
                while ( current.GetNeighbor( i ) != null )
                {
                    var results = this._comparer.Compare( current.GetNeighbor( i )!.TargetNode.Key!, value );

                    if ( results == 0 )
                    {
                        return true; // we found the item
                    }

                    if ( results < 0 )
                    {
                        current = current.GetNeighbor( i )!.TargetNode; // move on to the next neighbor
                    }
                    else
                    {
                        break; // exit while loop, we need to move down the height of the current node
                    }
                }
            }

            // if we reach here, we searched to the end of the list without finding the element
            return false;
        }

        /// <summary>
        /// Adds a new element to the SkipList.
        /// </summary>
        /// <param name="key">The value to add.</param>
        /// <param name="value"></param>
        /// <remarks>This SkipList implementation does not allow for duplicates.  Attempting to add a
        /// duplicate value will not raise an exception, it will simply exit the method without
        /// changing the SkipList.</remarks>
        void IDictionary<TKey, TValue>.Add( TKey key, TValue value ) => this.Add( key, value );

        public int Add( TKey key, TValue value ) => this.AddOrUpdate( key, value, false );

        private int AddOrUpdate( TKey key, TValue value, bool replace )
        {
            var updates = this.BuildUpdateTable( key, out var distanceFromHead );
            var current = updates[0].Node;

            // see if a duplicate is being inserted
            if ( current.GetNeighbor( 0 ) != null && this._comparer.Compare( current.GetNeighbor( 0 )!.TargetNode.Key!, key ) == 0 )
            {
                if ( replace )
                {
                    current.GetNeighbor( 0 )!.TargetNode.Value = value;

                    return -1;
                }

                throw new ArgumentException( "An element with the same key already exists." );
            }

            // create a new node
            var newNode = new Node( key, value, this.ChooseRandomHeight( this._head.Height + 1 ) );
            this.Count++;

            // if the node's level is greater than the head's level, increase the head's level
            if ( newNode.Height > this._head.Height )
            {
                this._head.IncrementHeight();
                this._head.SetNeighbor( this._head.Height - 1, new NodeLink( newNode, distanceFromHead ) );
            }

            // splice the new node into the list
            for ( var i = 0; i < updates.Length; i++ )
            {
                var update = updates[i];

                if ( i < newNode.Height )
                {
                    var replacedLink = update.Node.GetNeighbor( i );

                    if ( replacedLink != null )
                    {
                        newNode.SetNeighbor( i, new NodeLink( replacedLink.TargetNode, 1 + replacedLink.Width - update.Distance ) );
                    }

                    update.Node.SetNeighbor( i, new NodeLink( newNode, update.Distance ) );
                }
                else if ( update.Node.GetNeighbor( i ) != null )
                {
                    update.Node.GetNeighbor( i )!.Width++;
                }
            }

            return distanceFromHead - 1;
        }

        /// <summary>
        /// Attempts to remove a value from the SkipList.
        /// </summary>
        /// <param name="value">The value to remove from the SkipList.</param>
        /// <returns>True if the value is found and removed; false if the value is not found
        /// in the SkipList.</returns>
        public bool Remove( TKey value )
        {
            // There's a bug in Remove so let's try not to use it until necessary.
            throw new NotImplementedException();

#if FALSE
            var updates = this.BuildUpdateTable(value, out _);
            var current = updates[0].Node.GetNeighbor(0);

            if (current != null && this._comparer.Compare(current.TargetNode.Key, value) == 0)
            {
                this._count--;

                // We found the data to delete
                for (var i = 0; i < this._head.Height; i++)
                {
                    var update = updates[i];
                    
                    if (update.Node.GetNeighbor(i) == current)
                    {
                        var currentNeighbor = current.TargetNode.GetNeighbor(i);
                        if ( currentNeighbor != null )
                        {
                            update.Node.SetNeighbor( i, 
                                new NodeLink( currentNeighbor!.TargetNode,
                                update.Distance + currentNeighbor!.Width - 1 ) );
                        }
                        else
                        {
                            update.Node.SetNeighbor( i, null );
                        }
                    }
                }

                /*
                // finally, see if we need to trim the height of the list
                if (this._head[this._head.Height - 1] == null && this._head.Height > 1 )
                    // we removed the single, tallest item... reduce the list height
                {
                    this._head.DecrementHeight();
                }
*/
                
                return true; // the item was successfully removed
            }
            else
                // the data to delete wasn't found.
            {
                return false;
            }
#endif
        }

        public bool TryGetValue( TKey key, [NotNullWhen( true )] out TValue? value )
        {
            if ( this.TryFindNode( key, out var node, out _ ) )
            {
                value = node.Value!;

                return true;
            }

            value = default;

            return false;
        }

        public bool TryGetGreatestSmallerOrEqualValue( TKey key, [NotNullWhen( true )] out TValue? value )
        {
            if ( this.TryFindNode( key, out var node, out _, false ) )
            {
                value = node.Value!;

                return true;
            }

            value = default;

            return false;
        }

        public TValue this[ TKey key ]
        {
            get => this.Get( key );

            set => this.AddOrUpdate( key, value, true );
        }

        private TValue Get( TKey key )
        {
            if ( this.TryFindNode( key, out var node, out _ ) )
            {
                return node.Value;
            }

            throw new KeyNotFoundException();
        }

        public ICollection<TKey> Keys => throw new NotImplementedException();

        public ValueCollection Values => this._valueCollection ??= new ValueCollection( this );

        ICollection<TValue> IDictionary<TKey, TValue>.Values => throw new NotImplementedException();

        /// <summary>
        /// Gets the number of elements in the SkipList.
        /// </summary>
        public int Count { get; private set; }

        public bool IsReadOnly => false;

        /// <summary>
        /// Creates a table of the SkipListNode instances that will need to be updated when an item is
        /// added or removed from the SkipList.
        /// </summary>
        /// <param name="key">The value to be added or removed.</param>
        /// <param name="distanceFromHead"></param>
        /// <returns>An array of SkipListNode instances, as many as the height of the head node.
        /// A SkipListNode instance in array index k represents the SkipListNode at height k that must
        /// be updated following the addition/deletion.</returns>
        private NodeDistance[] BuildUpdateTable( TKey key, out int distanceFromHead )
        {
            var height = this._head.Height;

            Invariant.Assert( height > 0 );

            var updates = new NodeDistance[height];
            distanceFromHead = 1;

            var current = this._head;

            // determine the nodes that need to be updated at each level
            for ( var i = height - 1; i >= 0; i-- )
            {
                var width = 0;

                while ( current.GetNeighbor( i ) != null && this._comparer.Compare( current.GetNeighbor( i )!.TargetNode.Key, key ) < 0 )
                {
                    width += current.GetNeighbor( i )!.Width;
                    current = current.GetNeighbor( i )!.TargetNode;
                }

                distanceFromHead += width;

                for ( var j = height - 1; j > i; j-- )
                {
                    updates[j].Distance += width;
                }

                updates[i].Node = current;
                updates[i].Distance++;
            }

            return updates;
        }

        private Node GetNodeByIndex( int index )
        {
            index++;

            var currentNode = this._head;
            var currentIndex = 0;

            for ( var i = this._head.Height - 1; i >= 0; i-- )
            {
                while ( currentNode.GetNeighbor( i ) != null && currentIndex + currentNode.GetNeighbor( i )!.Width <= index )
                {
                    currentIndex += currentNode.GetNeighbor( i )!.Width;
                    currentNode = currentNode.GetNeighbor( i )!.TargetNode;
                }
            }

            return currentNode;
        }

        private bool TryFindNode( TKey key, [NotNullWhen( true )] out Node? node, out int position, bool exactMatchRequired = true )
        {
            var current = this._head;
            position = 0;

            for ( var i = this._head.Height - 1; i >= 0; i-- )
            {
                int comparisonResult;

                while ( current.GetNeighbor( i ) != null && (comparisonResult = this._comparer.Compare( current.GetNeighbor( i )!.TargetNode.Key, key )) <= 0 )
                {
                    position += current.GetNeighbor( i )!.Width;
                    current = current.GetNeighbor( i )!.TargetNode;

                    if ( comparisonResult == 0 )
                    {
                        node = current;
                        position -= 1;

                        return true;
                    }
                }
            }

            if ( exactMatchRequired || position == 0 )
            {
                node = null;
                position = -1;

                return false;
            }

            // We did not find the exact item, but we would one before.
            node = current;
            position--;

            return true;
        }

        /// <summary>
        /// Returns an enumerator to access the contents of the SkipList.
        /// </summary>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            // enumerate through the skip list one element at a time
            var current = this._head.GetNeighbor( 0 );

            while ( current != null )
            {
                yield return new KeyValuePair<TKey, TValue>( current.TargetNode.Key, current.TargetNode.Value );

                current = current.TargetNode.GetNeighbor( 0 );
            }
        }

        public IEnumerable<KeyValuePair<TKey, TValue>> GetItemsGreaterOrEqualThan( TKey key, bool returnsPrevious = false )
        {
            if ( !this.TryFindNode( key, out var node, out _, false ) )
            {
                yield break;
            }

            if ( returnsPrevious || this._comparer.Compare( node.Key, key ) >= 0 )
            {
                yield return new KeyValuePair<TKey, TValue>( node.Key, node.Value );
            }

            while ( (node = node.GetNeighbor( 0 )?.TargetNode) != null )
            {
                yield return new KeyValuePair<TKey, TValue>( node.Key, node.Value );
            }
        }

        /// <summary>
        /// This overridden form of ToString() is simply for displaying detailed information
        /// about the contents of the SkipList, used by SkipListTester - feel free to remove it.
        /// </summary>
        public override string ToString()
        {
            var current = this._head;
            var sb = new StringBuilder();

            while ( current != null )
            {
                sb.Append( "[ " );
                sb.Append( current );
                sb.Append( "];  " );

                if ( current.GetNeighbor( 0 ) == null )
                {
                    break;
                }

                current = current.GetNeighbor( 0 )?.TargetNode;
            }

            return sb.ToString();
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        public int IndexOfKey( TKey key )
        {
            if ( this.TryFindNode( key, out _, out var index ) )
            {
                return index;
            }

            return -1;
        }

        public int IndexOf( KeyValuePair<TKey, TValue> item )
        {
            if ( this.TryFindNode( item.Key, out var node, out var index ) && Equals( node.Value, item.Value ) )
            {
                return index;
            }

            return -1;
        }

        void IList<KeyValuePair<TKey, TValue>>.Insert( int index, KeyValuePair<TKey, TValue> item ) => throw new NotSupportedException();

        public void RemoveAt( int index ) => throw new NotImplementedException();

        public KeyValuePair<TKey, TValue> GetAt( int index )
        {
            if ( index < 0 || index >= this.Count )
            {
                throw new ArgumentOutOfRangeException( nameof(index) );
            }

            var node = this.GetNodeByIndex( index );

            return new KeyValuePair<TKey, TValue>( node.Key, node.Value );
        }

        KeyValuePair<TKey, TValue> IList<KeyValuePair<TKey, TValue>>.this[ int index ]
        {
            get => this.GetAt( index );
            set => throw new NotSupportedException();
        }

        private struct NodeDistance
        {
            public Node Node;
            public int Distance;
        }

        /// <summary>
        /// Represents a node in a SkipList.  A SkipListNode has a Height and a set of neighboring
        /// SkipListNodes (precisely as many neighbor references as its Height, although some neighbor
        /// references may be null).  Also, a SkipListNode contains some piece of data associated with it.
        /// </summary>
        private sealed class Node
        {
            private Node( NodeList neighbors )
            {
                this.Neighbors = neighbors;

                // This constructor is used for the head node only, the Key and Value properties are
                // never accessed for the head.
                this.Key = default!;
                this.Value = default!;
            }

            public static Node CreateHead() => new( new NodeList( 1 ) );

            public Node( TKey key, TValue value, int height )
            {
                if ( height <= 0 )
                {
                    throw new ArgumentOutOfRangeException( nameof(height) );
                }

                this.Neighbors = new NodeList( height );
                this.Key = key;
                this.Value = value;
            }

            public TKey Key { get; }

            public TValue Value { get; set; }

            private NodeList Neighbors { get; }

            /// <summary>
            /// Increases the height of the SkipListNode by 1.
            /// </summary>
            internal void IncrementHeight()
                =>

                    // Increase height by 1
                    this.Neighbors.IncrementHeight();

            /*
            /// <summary>
            /// Decreases the height of the SkipListNode by 1.
            /// </summary>
            internal void DecrementHeight() =>
                // Decrease height by 1
                this.Neighbors.DecrementHeight();

            */

            /// <summary>
            /// Gets the height of the SkipListNode.
            /// </summary>
            public int Height => this.Neighbors.Count;

            /// <summary>
            /// Provides indexed access to the neighbors of the SkipListNode.
            /// </summary>
            public void SetNeighbor( int index, NodeLink? value ) => this.Neighbors[index] = value;

            /// <summary>
            /// Provides indexed access to the neighbors of the SkipListNode.
            /// </summary>
            public NodeLink? GetNeighbor( int index ) => this.Neighbors[index];

            public override string ToString()
            {
                var sb = new StringBuilder();

                sb.AppendFormat( CultureInfo.InvariantCulture, "Key={{{0}}}, Height={{{1}}}, Nodes={{", this.Key, this.Height );

                for ( var i = 0; i < this.Height; i++ )
                {
                    if ( i != 0 )
                    {
                        sb.Append( ", " );
                    }

                    var neighbor = this.GetNeighbor( i );

                    if ( neighbor == null )
                    {
                        sb.Append( "null" );
                    }
                    else
                    {
                        sb.Append( string.Format( CultureInfo.InvariantCulture, "(Key={{{0}}}, Width={1})", neighbor.TargetNode.Key, neighbor.Width ) );
                    }
                }

                sb.Append( '}' );

                return sb.ToString();
            }
        }

        private sealed class NodeLink
        {
            public NodeLink( Node targetNode, int width )
            {
                this.TargetNode = targetNode;
                this.Width = width;
            }

            public Node TargetNode { get; }

            private int _width;

            public int Width
            {
                get => this._width;
                set
                {
                    if ( value == 0 )
                    {
                        throw new ArgumentOutOfRangeException( nameof(value) );
                    }

                    this._width = value;
                }
            }
        }

        /// <summary>
        /// Represents a collection of SkipListNodes.  This class differs from the base class - NodeList -
        /// in that it contains an internal method to increment or decrement the height of the SkipListNodeList.
        /// Incrementing the height adds a new neighbor to the list, decrementing the height removes the
        /// top-most neighbor.
        /// </summary>
        private sealed class NodeList : List<NodeLink?>
        {
            public NodeList( int height )
            {
                if ( height <= 0 )
                {
                    throw new ArgumentOutOfRangeException( nameof(height) );
                }

                // Add the specified number of items
                for ( var i = 0; i < height; i++ )
                {
                    this.Add( null );
                }
            }

            /// <summary>
            /// Increases the size of the SkipListNodeList by one, adding a default SkipListNode.
            /// </summary>
            internal void IncrementHeight()
                =>

                    // add a dummy entry
                    this.Add( null );

            /*
            /// <summary>
            /// Decreases the size of the SkipListNodeList by one, removing the "top-most" SkipListNode.
            /// </summary>
            internal void DecrementHeight()
            {
                if ( this.Count == 1 )
                {
                    throw new InvalidOperationException();
                }

                this.RemoveAt( this.Count - 1 );
            }
            */
        }

        public sealed class ValueCollection : IReadOnlyList<TValue>
        {
            private readonly SkipListDictionary<TKey, TValue> _parent;

            public ValueCollection( SkipListDictionary<TKey, TValue> parent )
            {
                this._parent = parent;
            }

            public IEnumerator<TValue> GetEnumerator()
            {
                // enumerate through the skip list one element at a time
                var current = this._parent._head.GetNeighbor( 0 );

                while ( current != null )
                {
                    yield return current.TargetNode.Value;

                    current = current.TargetNode.GetNeighbor( 0 );
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

            public int Count => this._parent.Count;

            public TValue this[ int index ] => this._parent.GetAt( index ).Value;
        }
    }
}