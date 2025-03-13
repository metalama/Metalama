// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Engine.Utilities.Roslyn
{
    public static class NodeFinder
    {
        public static bool TryFindOldNodeInNewTree( SyntaxNode oldNode, SyntaxTree newTree, out SyntaxNode newNode )
            => TryFindOldNodeInNewRoot( oldNode, newTree.GetRoot(), out newNode );

        public static bool TryFindOldNodeInNewRoot( SyntaxNode oldNode, SyntaxNode newRoot, out SyntaxNode newNode )
        {
            // Create a stack with the position of each ancestor node with respect to its parent.
            // We only position ourselves with respect to other nodes of the same kind, as we want to be less sensitive to changes in the new
            // syntax tree.

            Stack<(SyntaxKind Kind, int Position)> stack = new();

            for ( var oldNodeCursor = oldNode; oldNodeCursor?.Parent != null; oldNodeCursor = oldNodeCursor.Parent )
            {
                var syntaxKind = oldNodeCursor.Kind();
                var childrenOfSameKind = oldNodeCursor.Parent.ChildNodes().Where( n => n.IsKind( syntaxKind ) );

                var index = 0;

                foreach ( var childOfSameKind in childrenOfSameKind )
                {
                    if ( childOfSameKind == oldNodeCursor )
                    {
                        stack.Push( (syntaxKind, index) );
                        index = -1;

                        break;
                    }

                    index++;
                }

                if ( index != -1 )
                {
                    // We should have found ourselves in the parent node.
                    throw new AssertionFailedException( $"Cannot find the parent node of {oldNodeCursor} at '{oldNodeCursor.GetLocation()}'." );
                }
            }

            // Navigate the inverted stack from the new root.
            newNode = newRoot;

            while ( stack.Count > 0 )
            {
                var slice = stack.Pop();
                var childrenOfSameKind = newNode.ChildNodes().Where( n => n.IsKind( slice.Kind ) );
                var childAtPosition = childrenOfSameKind.ElementAtOrDefault( slice.Position );

                if ( childAtPosition == null )
                {
                    return false;
                }
                else
                {
                    newNode = childAtPosition;
                }
            }

            return true;
        }
    }
}