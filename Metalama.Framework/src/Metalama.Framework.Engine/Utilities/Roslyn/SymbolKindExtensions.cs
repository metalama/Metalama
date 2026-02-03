// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.Utilities.Roslyn;

/// <summary>
/// Extension methods for <see cref="SymbolKind"/>.
/// </summary>
internal static class SymbolKindExtensions
{
    /// <summary>
    /// Determines whether the symbol kind represents a type symbol
    /// (named type, array type, pointer type, function pointer type, dynamic type, error type, or type parameter).
    /// </summary>
    public static bool IsType( this SymbolKind kind )
        => kind is SymbolKind.NamedType or SymbolKind.ArrayType or SymbolKind.PointerType
            or SymbolKind.FunctionPointerType or SymbolKind.DynamicType or SymbolKind.ErrorType or SymbolKind.TypeParameter;
}
