// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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