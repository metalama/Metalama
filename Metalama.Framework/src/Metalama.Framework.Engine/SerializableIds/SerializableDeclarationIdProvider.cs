// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Microsoft.CodeAnalysis;
using System;

namespace Metalama.Framework.Engine.SerializableIds;

public static partial class SerializableDeclarationIdProvider
{
    private const string _assemblyPrefix = "Assembly:";

    /// <summary>
    /// The marker that introduces the discriminator of a file-local type in a <see cref="SerializableDeclarationId"/>.
    /// </summary>
    private const string _fileLocalTypeMarker = ";File=";

    private static readonly char[] _separators = [';', '='];

    /// <summary>
    /// Returns the metadata name of the file-local type that contains the given symbol, or <c>null</c> when the symbol
    /// is neither a file-local type nor contained in one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A documentation comment identifier names a type by its namespace and its name only, and two file-local types
    /// declared in two files share both, so the identifier of such a declaration needs a discriminator. The metadata
    /// name that the compiler gives a file-local type, of the form <c>&lt;File&gt;F&lt;checksum&gt;__Name</c>, is that
    /// discriminator. The checksum is computed by the compiler from the path of the declaring file, after the
    /// normalization that the <c>pathmap</c> option performs, and the resulting name is what the compiler writes into
    /// the assembly. An identifier built from it is therefore stable exactly as far as the emitted assembly is: it is
    /// identical on every machine in a deterministic build, and specific to the local path otherwise.
    /// </para>
    /// <para>
    /// The <c>file</c> modifier is only allowed on a top-level type, so at most one type in a containing chain is
    /// file-local.
    /// </para>
    /// </remarks>
    private static string? GetFileLocalTypeMetadataName( ISymbol symbol )
    {
        var current = symbol as INamedTypeSymbol ?? symbol.ContainingType;

        while ( current != null )
        {
            if ( current.IsFileLocal )
            {
                return current.MetadataName;
            }

            current = current.ContainingType;
        }

        return null;
    }

    /// <summary>
    /// Returns the metadata name of the file-local type that contains the given declaration, or <c>null</c> when the
    /// declaration is neither a file-local type nor contained in one.
    /// </summary>
    /// <remarks>
    /// An introduced declaration has no symbol of its own, but the type that declares it can still be a file-local type
    /// of the source code, because the <c>file</c> modifier cannot be applied to an introduced type. The topmost
    /// declaring type is therefore used when the declaration itself has no symbol.
    /// </remarks>
    private static string? GetFileLocalTypeMetadataName( IDeclaration declaration )
    {
        if ( declaration.GetSymbol() is { } symbol )
        {
            return GetFileLocalTypeMetadataName( symbol );
        }

        if ( declaration.GetTopmostNamedType()?.GetSymbol() is { } topmostTypeSymbol )
        {
            return GetFileLocalTypeMetadataName( topmostTypeSymbol );
        }

        return null;
    }

    /// <summary>
    /// Returns the given identifier with the discriminator of a file-local type appended to it, or the identifier
    /// itself when <paramref name="fileLocalTypeMetadataName"/> is <c>null</c>.
    /// </summary>
    /// <remarks>
    /// A declaration that does not belong to a file-local type appends nothing, so every identifier written before this
    /// discriminator existed keeps its exact string and resolves as it did.
    /// </remarks>
    private static SerializableDeclarationId CreateId( string id, string? fileLocalTypeMetadataName )
        => new(
            fileLocalTypeMetadataName == null
                ? id
                : id + _fileLocalTypeMarker + fileLocalTypeMetadataName );

    /// <summary>
    /// Returns the given identifier with the discriminator of a file-local type removed, and reports the discriminator
    /// that was removed.
    /// </summary>
    /// <remarks>
    /// The discriminator is removed before the identifier is parsed, so that the rest of the parsing sees exactly the
    /// string that a declaration outside a file-local type would have produced. A <see cref="SerializableTypeId"/> is
    /// left untouched, because it embeds a whole declaration identifier as its generic context and that embedded
    /// identifier can carry a discriminator of its own.
    /// </remarks>
    private static SerializableDeclarationId StripFileLocalType( this SerializableDeclarationId id, out string? fileLocalTypeMetadataName )
    {
        var idString = id.Id;

        if ( !SerializableTypeId.IsTypeId( idString ) )
        {
            var index = idString.IndexOf( _fileLocalTypeMarker, StringComparison.Ordinal );

            if ( index >= 0 )
            {
                fileLocalTypeMetadataName = idString.Substring( index + _fileLocalTypeMarker.Length );

                return new SerializableDeclarationId( idString.Substring( 0, index ) );
            }
        }

        fileLocalTypeMetadataName = null;

        return id;
    }

    /// <summary>
    /// Determines whether a symbol found by a documentation comment identifier belongs to the file-local type that the
    /// identifier named, if any.
    /// </summary>
    /// <remarks>
    /// The test is symmetric: an identifier that carries no discriminator refuses every candidate that belongs to a
    /// file-local type, and an identifier that carries one refuses every candidate that does not belong to that exact
    /// type. Resolving to a type of the same name declared in another file would be worse than not resolving at all.
    /// </remarks>
    private static bool MatchesFileLocalType( ISymbol candidate, string? fileLocalTypeMetadataName )
        => GetFileLocalTypeMetadataName( candidate ) == fileLocalTypeMetadataName;

    /// <summary>
    /// Determines whether a declaration found by a documentation comment identifier belongs to the file-local type that
    /// the identifier named, if any.
    /// </summary>
    /// <remarks>
    /// <see cref="MatchesFileLocalType(ISymbol,string)"/> describes the rule.
    /// </remarks>
    private static bool MatchesFileLocalType( IDeclaration candidate, string? fileLocalTypeMetadataName )
        => GetFileLocalTypeMetadataName( candidate ) == fileLocalTypeMetadataName;
}
