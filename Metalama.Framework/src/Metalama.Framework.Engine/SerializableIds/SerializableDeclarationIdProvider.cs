// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
using System.IO.Hashing;
using System.Linq;
using System.Text;

namespace Metalama.Framework.Engine.SerializableIds;

public static partial class SerializableDeclarationIdProvider
{
    private const string _assemblyPrefix = "Assembly:";
    internal const char FileLocalSeparator = '|';

    private static readonly char[] _separators = [';', '='];

    /// <summary>
    /// For a symbol that is a file-local type or is contained within a file-local type,
    /// returns a 64-bit hash of the source file path (hex formatted). Otherwise returns <c>null</c>.
    /// </summary>
    internal static string? GetFileLocalHash( ISymbol symbol )
    {
        var current = symbol.Kind == SymbolKind.NamedType && symbol is INamedTypeSymbol ? symbol : symbol.ContainingType;

        while ( current != null && current.Kind == SymbolKind.NamedType && current is INamedTypeSymbol namedType )
        {
            if ( namedType.IsFileLocal )
            {
                var filePath = namedType.DeclaringSyntaxReferences.FirstOrDefault()?.SyntaxTree.FilePath;

                return filePath == null ? null : HashFilePath( filePath );
            }

            current = namedType.ContainingType;
        }

        return null;
    }

    /// <summary>
    /// For a declaration that is a file-local type or is contained within a file-local type,
    /// returns a 64-bit hash of the source file path (hex formatted). Otherwise returns <c>null</c>.
    /// </summary>
    internal static string? GetFileLocalHash( IDeclaration declaration )
    {
        if ( declaration.GetSymbol() is { } symbol )
        {
            return GetFileLocalHash( symbol );
        }

        return null;
    }

    /// <summary>
    /// Computes a 64-bit XxHash64 of the file path and returns it as a lowercase hex string.
    /// </summary>
    private static string HashFilePath( string filePath )
    {
        var bytes = Encoding.UTF8.GetBytes( filePath );
        var hash = XxHash64.HashToUInt64( bytes );

        return hash.ToString( "x16", System.Globalization.CultureInfo.InvariantCulture );
    }

    /// <summary>
    /// Splits a serializable ID into the base documentation ID and an optional file-local hash.
    /// </summary>
    internal static (string BaseId, string? FileLocalHash) ParseFileLocalSuffix( string idString )
    {
        var pipeIndex = idString.IndexOfOrdinal( FileLocalSeparator );

        if ( pipeIndex < 0 )
        {
            return (idString, null);
        }

        // The hash is always a fixed-length hex string (16 chars), so we can simply
        // extract it. Format: "baseDocId|hash" or "baseDocId|hash;kind=ordinal"
        var semiAfterPipe = idString.IndexOf( ';', pipeIndex );

        if ( semiAfterPipe > 0 )
        {
            var hash = idString.Substring( pipeIndex + 1, semiAfterPipe - pipeIndex - 1 );
            var baseId = idString.Substring( 0, pipeIndex ) + idString.Substring( semiAfterPipe );

            return (baseId, hash);
        }
        else
        {
            var hash = idString.Substring( pipeIndex + 1 );
            var baseId = idString.Substring( 0, pipeIndex );

            return (baseId, hash);
        }
    }

    /// <summary>
    /// Appends the file-local hash suffix to an ID string if the hash is not null.
    /// </summary>
    internal static string AppendFileLocalSuffix( string idString, string? fileLocalHash )
    {
        if ( fileLocalHash == null )
        {
            return idString;
        }

        return idString + FileLocalSeparator + fileLocalHash;
    }
}