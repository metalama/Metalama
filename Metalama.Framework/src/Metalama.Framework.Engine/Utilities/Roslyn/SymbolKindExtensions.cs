// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.Utilities.Roslyn;

/// <summary>
/// Extension properties for <see cref="SymbolKind"/>.
/// </summary>
public static class SymbolKindExtensions
{
    extension( SymbolKind kind )
    {
        /// <summary>
        /// Gets a value indicating whether the symbol kind represents a type symbol
        /// (named type, array type, pointer type, function pointer type, dynamic type, error type, or type parameter).
        /// </summary>
        public bool IsType
            => kind is SymbolKind.NamedType or SymbolKind.ArrayType or SymbolKind.PointerType
                or SymbolKind.FunctionPointerType or SymbolKind.DynamicType or SymbolKind.ErrorType or SymbolKind.TypeParameter;

        /// <summary>
        /// Gets a value indicating whether the symbol kind represents a member
        /// (method, property, field, or event).
        /// </summary>
        public bool IsMember => kind is SymbolKind.Method or SymbolKind.Property or SymbolKind.Field or SymbolKind.Event;

        /// <summary>
        /// Gets a value indicating whether the symbol kind represents a non-named type
        /// (array, dynamic, error, function pointer, or pointer type).
        /// </summary>
        public bool IsNonNamedType
            => kind is SymbolKind.ArrayType or SymbolKind.DynamicType or SymbolKind.ErrorType
                or SymbolKind.FunctionPointerType or SymbolKind.PointerType;
    }
}