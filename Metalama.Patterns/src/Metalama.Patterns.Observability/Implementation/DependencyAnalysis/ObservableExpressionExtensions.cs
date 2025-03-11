// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

// ReSharper disable UnusedMember.Global

namespace Metalama.Patterns.Observability.Implementation.DependencyAnalysis;

[CompileTime]
internal static class ObservableExpressionExtensions
{
    /// <summary>
    /// Gets the ancestors of the current node in leaf-to-root order.
    /// </summary>
    public static IEnumerable<T> Ancestors<T>( this T node )
        where T : ObservableExpression
        => AncestorsCore( node, false );

    /// <summary>
    /// Gets the current node and its ancestors in leaf-to-root order.
    /// </summary>
    public static IEnumerable<T> AncestorsAndSelf<T>( this T node )
        where T : ObservableExpression
        => AncestorsCore( node, true );

    private static IEnumerable<T> AncestorsCore<T>( T node, bool includeSelf )
        where T : ObservableExpression
    {
        if ( includeSelf )
        {
            yield return node;
        }

        while ( node.Parent != null )
        {
            node = (T) node.Parent;

            yield return node;
        }
    }
}