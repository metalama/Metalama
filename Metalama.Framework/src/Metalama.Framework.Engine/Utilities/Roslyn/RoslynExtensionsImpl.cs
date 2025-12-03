// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.Utilities.Roslyn;

/// <summary>
/// Implementation of <see cref="IRoslynExtensions"/> that delegates to the static extension methods
/// in <see cref="SymbolExtensions"/>. This service uses conditional compilation to handle
/// Roslyn version-specific APIs.
/// </summary>
internal sealed class RoslynExtensionsImpl : IRoslynExtensions
{
    /// <summary>
    /// Gets the singleton instance of the service.
    /// </summary>
    public static RoslynExtensionsImpl Instance { get; } = new();

    private RoslynExtensionsImpl() { }

    /// <inheritdoc />
    public SyntaxReference? GetPrimarySyntaxReference( ISymbol? symbol ) => symbol.GetPrimarySyntaxReference();

    /// <inheritdoc />
    public SyntaxNode? GetPrimaryDeclarationSyntax( ISymbol symbol ) => symbol.GetPrimaryDeclarationSyntax();
}