// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Collections.Generic;

namespace Metalama.Framework.Engine.AspectOrdering
{
    internal sealed class DirectedGraph
    {
        private readonly SimpleLinkedListNode<int>?[] _successors;
        private readonly SimpleLinkedListNode<int>?[] _predecessors;

        public const int NotDiscovered = int.MaxValue;
        private const int _cycle = int.MinValue;

        private readonly int _size;

        public int[] GetInitialVector()
        {
            var n = this._size;
            var vector = new int[n];

            for ( var i = 0; i < n; i++ )
            {
                vector[i] = NotDiscovered;
            }

            return vector;
        }

        public DirectedGraph( int size )
        {
            this._size = size;
            this._successors = new SimpleLinkedListNode<int>[size];
            this._predecessors = new SimpleLinkedListNode<int>[size];
        }

        public void AddEdge( int predecessor, int successor )
        {
            SimpleLinkedListNode<int>.Insert( ref this._successors[predecessor], successor );
            SimpleLinkedListNode<int>.Insert( ref this._predecessors[successor], predecessor );
        }

        public int DoBreadthFirstSearch( int initialNode, int[] distances, int[] directPredecessors )
        {
            var n = distances.Length;

            distances[initialNode] = 0;

            Queue<NodeInfo> queue = new( n );
            queue.Enqueue( new NodeInfo { Node = initialNode, NodesInPath = new SimpleLinkedListNode<int>( initialNode, null ) } );

            while ( queue.Count > 0 )
            {
                var nodeInfo = queue.Dequeue();
                var current = nodeInfo.Node;
                var successorNode = this._successors[current];
                var currentDistance = distances[current];

                while ( successorNode != null )
                {
                    var successor = successorNode.Value;
                    var successorDistance = distances[successor];
                    var newSuccessorDistance = currentDistance + 1;

                    // Check that the new node is not already in the path.
                    var hasCycle = false;
                    var nodeInPathCursor = nodeInfo.NodesInPath;

                    while ( nodeInPathCursor != null )
                    {
                        if ( nodeInPathCursor.Value == successor )
                        {
                            hasCycle = true;

                            break;
                        }

                        nodeInPathCursor = nodeInPathCursor.Next;
                    }

                    if ( hasCycle )
                    {
                        // We just discovered that the successor is part of a cycle.
                        distances[successor] = _cycle;
                        directPredecessors[successor] = current;

                        return successor;
                    }

                    if ( successorDistance == NotDiscovered || successorDistance < newSuccessorDistance )
                    {
                        distances[successor] = newSuccessorDistance;
                        directPredecessors[successor] = current;

                        queue.Enqueue( new NodeInfo { Node = successor, NodesInPath = new SimpleLinkedListNode<int>( successor, nodeInfo.NodesInPath ) } );
                    }
                    else if ( successorDistance == _cycle )
                    {
                        // We have already discovered that the successor is part of a cycle.
                    }

                    successorNode = successorNode.Next;
                }
            }

            return -1;
        }

        private struct NodeInfo
        {
            public int Node;
            public SimpleLinkedListNode<int> NodesInPath;
        }

        private sealed class SimpleLinkedListNode<T>
        {
            public static void Insert( ref SimpleLinkedListNode<T>? node, T value ) => node = new SimpleLinkedListNode<T>( value, node );

            public SimpleLinkedListNode( T value, SimpleLinkedListNode<T>? next )
            {
                this.Value = value;
                this.Next = next;
            }

            public T? Value { get; }

            public SimpleLinkedListNode<T>? Next { get; }
        }
    }
}