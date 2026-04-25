// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.SerializableIds;

public static partial class SerializableDeclarationIdProvider
{
    private const string _assemblyPrefix = "Assembly:";

    private static readonly char[] _separators = [';', '='];

    /// <summary>
    /// Determines whether a symbol is a file-local type or is contained within a file-local type.
    /// </summary>
    private static bool IsInFileLocalType( ISymbol symbol )
    {
        var current = symbol is INamedTypeSymbol ? symbol : symbol.ContainingType;

#pragma warning disable LAMA0860 // This checks Roslyn symbols, not Metalama declarations
        while ( current is INamedTypeSymbol namedType )
#pragma warning restore LAMA0860
        {
            if ( namedType.IsFileLocal )
            {
                return true;
            }

            current = namedType.ContainingType;
        }

        return false;
    }

    /// <summary>
    /// Determines whether a declaration is a file-local type or is contained within a file-local type.
    /// </summary>
    private static bool IsInFileLocalType( IDeclaration declaration )
    {
        if ( declaration.GetSymbol() is { } symbol )
        {
            return IsInFileLocalType( symbol );
        }

        return false;
    }
}