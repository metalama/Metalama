// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Extensibility.Transformations;

/// <summary>
/// Compares syntax nodes by reference, because a redirection is keyed by the identity of its source node.
/// </summary>
internal sealed class SyntaxNodeReferenceComparer : IEqualityComparer<SyntaxNode>
{
    public static SyntaxNodeReferenceComparer Instance { get; } = new();

    private SyntaxNodeReferenceComparer() { }

    public bool Equals( SyntaxNode? x, SyntaxNode? y ) => ReferenceEquals( x, y );

    public int GetHashCode( SyntaxNode obj ) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode( obj );
}
