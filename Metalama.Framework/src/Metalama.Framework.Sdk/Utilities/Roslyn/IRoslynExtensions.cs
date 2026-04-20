// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.Utilities.Roslyn;

/// <summary>
/// Provides extension methods for <see cref="ISymbol"/> that require version-specific Roslyn API support.
/// This interface abstracts over Roslyn version differences (e.g., partial properties in Roslyn 4.12+,
/// partial events in Roslyn 5.0+).
/// </summary>
internal interface IRoslynExtensions
{
    /// <summary>
    /// Gets the primary syntax reference for a symbol. For partial methods, properties, or events,
    /// returns the implementation part if available; otherwise returns the definition part.
    /// </summary>
    SyntaxReference? GetPrimarySyntaxReference( ISymbol? symbol );

    /// <summary>
    /// Gets the primary declaration syntax node for a symbol.
    /// </summary>
    SyntaxNode? GetPrimaryDeclarationSyntax( ISymbol symbol );
}