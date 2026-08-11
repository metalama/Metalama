// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Microsoft.CodeAnalysis;
using System;

namespace Metalama.Framework.Engine.SerializableIds;

public static partial class SerializableDeclarationIdProvider
{
    private const string _nullableSuffix = ";Nullable";
    private const string _nullObliviousSuffix = ";NullOblivious";

    /// <summary>
    /// Returns the given identifier with the nullable annotation of a named type appended to it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A <see cref="SerializableDeclarationId"/> names a declaration, and the nullable annotation is part of a type
    /// rather than of the declaration it comes from, so the documentation identifier cannot express it. It is appended
    /// after a semicolon, which is how the identifier already carries a <c>RefTargetKind</c>, and stripped again when
    /// the identifier is resolved.
    /// </para>
    /// <para>
    /// A non-nullable type appends nothing, so every identifier written before this existed keeps its exact string and
    /// resolves as it did. The annotation has to be part of the string rather than of the reference that holds it,
    /// because a durable reference is identified by its identifier alone: one rebuilt from the string would otherwise
    /// resolve to the non-nullable type. See issue #1840.
    /// </para>
    /// </remarks>
    internal static SerializableDeclarationId WithNullability( this SerializableDeclarationId id, bool? isNullable )
        => isNullable switch
        {
            false => id,
            true => new SerializableDeclarationId( id.Id + _nullableSuffix ),
            null => new SerializableDeclarationId( id.Id + _nullObliviousSuffix )
        };

    /// <summary>
    /// Returns the given identifier with the nullable annotation removed, and reports the annotation that was removed.
    /// </summary>
    internal static SerializableDeclarationId StripNullability( this SerializableDeclarationId id, out bool? isNullable )
    {
        var idString = id.Id;

        if ( idString.EndsWith( _nullableSuffix, StringComparison.Ordinal ) )
        {
            isNullable = true;

            return new SerializableDeclarationId( idString.Substring( 0, idString.Length - _nullableSuffix.Length ) );
        }

        if ( idString.EndsWith( _nullObliviousSuffix, StringComparison.Ordinal ) )
        {
            isNullable = null;

            return new SerializableDeclarationId( idString.Substring( 0, idString.Length - _nullObliviousSuffix.Length ) );
        }

        isNullable = false;

        return id;
    }

    /// <summary>
    /// Applies to a declaration the nullable annotation that its identifier carried, which is meaningful for a named
    /// type alone.
    /// </summary>
    private static ICompilationElement? ApplyNullability( ICompilationElement? declaration, bool? isNullable )
    {
        if ( isNullable == false || declaration == null
                                 || declaration.DeclarationKind != DeclarationKind.NamedType
                                 || declaration is not INamedType namedType )
        {
            return declaration;
        }

        return isNullable == true ? namedType.ToNullable() : namedType.StripNullabilityAnnotation();
    }

    /// <summary>
    /// Applies to a symbol the nullable annotation that its identifier carried, which is meaningful for a named type
    /// alone.
    /// </summary>
    private static ISymbol? ApplyNullability( ISymbol? symbol, bool? isNullable )
    {
        if ( isNullable == false || symbol == null || symbol.Kind != SymbolKind.NamedType || symbol is not INamedTypeSymbol namedTypeSymbol )
        {
            return symbol;
        }

        return namedTypeSymbol.WithNullableAnnotation( isNullable == true ? NullableAnnotation.Annotated : NullableAnnotation.None );
    }
}
