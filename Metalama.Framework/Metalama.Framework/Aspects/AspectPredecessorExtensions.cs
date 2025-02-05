// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Collections.Generic;

namespace Metalama.Framework.Aspects;

/// <summary>
/// Extension methods for <see cref="IAspectPredecessor"/>.
/// </summary>
public static class AspectPredecessorExtensions
{
    /// <summary>
    /// Gets the roots of the predecessor tree. A root is a predecessor that does not itself have a predecessor.
    /// </summary>
    public static IReadOnlyList<IAspectPredecessor> GetRoots( this IAspectPredecessor predecessor )
    {
        if ( predecessor.Predecessors.IsDefaultOrEmpty )
        {
            return new[] { predecessor };
        }

        var list = new List<IAspectPredecessor>();
        ProcessRecursive( predecessor );

        return list;

        void ProcessRecursive( IAspectPredecessor p )
        {
            if ( p.Predecessors.IsDefaultOrEmpty )
            {
                list.Add( p );
            }
            else
            {
                foreach ( var child in p.Predecessors )
                {
                    ProcessRecursive( child.Instance );
                }
            }
        }
    }
}