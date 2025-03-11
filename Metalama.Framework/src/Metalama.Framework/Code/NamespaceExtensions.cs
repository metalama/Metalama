// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Code;

[CompileTime]
public static class NamespaceExtensions
{
    /// <summary>
    /// Gets a value indicating whether the current namespace is the ancestor of another given namespace.
    /// This method returns <c>false</c> when both namespaces are equal.
    /// </summary>
    public static bool IsAncestorOf( this INamespace a, INamespace b )
    {
        if ( a.IsGlobalNamespace )
        {
            return !b.IsGlobalNamespace;
        }

        var aFullName = a.FullName;
        var bFullName = b.FullName;

        return bFullName.StartsWith( aFullName, StringComparison.Ordinal ) && bFullName.Length > aFullName.Length && bFullName[aFullName.Length] == '.';
    }

    /// <summary>
    /// Gets a value indicating whether the current namespace is the descendant of another given namespace.
    /// This method returns <c>false</c> when both namespaces are equal.
    /// </summary>
    public static bool IsDescendantOf( this INamespace a, INamespace b ) => b.IsAncestorOf( a );

    /// <summary>
    /// Gets all child and descendant namespaces of the current namespace, excluding the current namespace. 
    /// </summary>
    public static IReadOnlyList<INamespace> Descendants( this INamespace ns )
    {
        var list = new List<INamespace>();

        void ProcessRecursive( INamespace n )
        {
            foreach ( var child in n.Namespaces )
            {
                list.Add( child );

                ProcessRecursive( child );
            }
        }

        ProcessRecursive( ns );

        return list;
    }

    /// <summary>
    /// Gets all child and descendant namespaces of the current namespace, plus the current namespace. 
    /// </summary>
    public static IReadOnlyList<INamespace> DescendantsAndSelf( this INamespace ns )
    {
        var list = new List<INamespace>();

        void ProcessRecursive( INamespace n )
        {
            list.Add( n );

            foreach ( var child in n.Namespaces )
            {
                ProcessRecursive( child );
            }
        }

        ProcessRecursive( ns );

        return list;
    }
}