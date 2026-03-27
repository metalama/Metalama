// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
using System.Linq;

namespace Metalama.Framework.Engine.SerializableIds;

public static partial class SerializableDeclarationIdProvider
{
    private const string _assemblyPrefix = "Assembly:";
    internal const char FileLocalSeparator = '|';

    private static readonly char[] _separators = [';', '='];

    /// <summary>
    /// For a symbol that is a file-local type or is contained within a file-local type,
    /// returns the source file path. Otherwise returns <c>null</c>.
    /// </summary>
    internal static string? GetFileLocalFilePath( ISymbol symbol )
    {
        var current = symbol.Kind == SymbolKind.NamedType && symbol is INamedTypeSymbol ? symbol : symbol.ContainingType;

        while ( current != null && current.Kind == SymbolKind.NamedType && current is INamedTypeSymbol namedType )
        {
            if ( namedType.IsFileLocal )
            {
                return namedType.DeclaringSyntaxReferences.FirstOrDefault()?.SyntaxTree.FilePath;
            }

            current = namedType.ContainingType;
        }

        return null;
    }

    /// <summary>
    /// For a declaration that is a file-local type or is contained within a file-local type,
    /// returns the source file path. Otherwise returns <c>null</c>.
    /// </summary>
    internal static string? GetFileLocalFilePath( IDeclaration declaration )
    {
        if ( declaration.GetSymbol() is { } symbol )
        {
            return GetFileLocalFilePath( symbol );
        }

        return null;
    }

    /// <summary>
    /// Splits a serializable ID into the base documentation ID and an optional file-local file path.
    /// </summary>
    internal static (string BaseId, string? FileLocalPath) ParseFileLocalSuffix( string idString )
    {
        var pipeIndex = idString.IndexOfOrdinal( FileLocalSeparator );

        if ( pipeIndex < 0 )
        {
            return (idString, null);
        }

        // The | can appear before or after a ; separator.
        // Format: "baseDocId|filePath" or "baseDocId|filePath;kind=ordinal"
        var semiAfterPipe = idString.IndexOf( ';', pipeIndex );

        if ( semiAfterPipe > 0 )
        {
            var filePath = idString.Substring( pipeIndex + 1, semiAfterPipe - pipeIndex - 1 );
            var baseId = idString.Substring( 0, pipeIndex ) + idString.Substring( semiAfterPipe );

            return (baseId, filePath);
        }
        else
        {
            var filePath = idString.Substring( pipeIndex + 1 );
            var baseId = idString.Substring( 0, pipeIndex );

            return (baseId, filePath);
        }
    }

    /// <summary>
    /// Appends the file-local suffix to an ID string if the file path is not null.
    /// </summary>
    internal static string AppendFileLocalSuffix( string idString, string? fileLocalPath )
    {
        if ( fileLocalPath == null )
        {
            return idString;
        }

        return idString + FileLocalSeparator + fileLocalPath;
    }
}