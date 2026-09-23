// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using System;
using System.Globalization;
using System.Linq;

namespace Metalama.Framework.Engine.SerializableIds;

public static partial class SerializableDeclarationIdProvider
{
    [PublicAPI]
    public static ISymbol ResolveToSymbol( this SerializableDeclarationId id, CompilationContext compilationContext )
    {
        // Note that the symbol resolution can fail for methods when the method signature contains a type from a missing assembly.

        var symbol = id.ResolveToSymbolOrNull( compilationContext )
                     ??
                     throw new AssertionFailedException( $"Cannot get a symbol for '{id}'." );

        return symbol;
    }

    public static ISymbol? ResolveToSymbolOrNull( this SerializableDeclarationId id, CompilationContext compilationContext )
    {
        var symbol = id.ResolveToSymbolOrNull( compilationContext, out var isReturnParameter );

        return isReturnParameter ? null : symbol;
    }

    public static ISymbol? ResolveToSymbolOrNull( this SerializableDeclarationId id, CompilationContext compilationContext, out bool isReturnParameter )
    {
        // The nullable annotation is carried by the identifier but is not part of the documentation identifier that
        // names the declaration, so it is removed before the lookup and applied to its result.
        var idWithoutNullability = id.StripNullability( out var isNullable );

        return ApplyNullability( ResolveToSymbolOrNullCore( idWithoutNullability, compilationContext, out isReturnParameter ), isNullable );
    }

    private static ISymbol? ResolveToSymbolOrNullCore(
        SerializableDeclarationId id,
        CompilationContext compilationContext,
        out bool isReturnParameter )
    {
        var compilation = compilationContext.Compilation;

        isReturnParameter = false;

        // The discriminator of a file-local type is removed before the identifier is parsed, so that the rest of the
        // parsing sees the string that a declaration outside a file-local type would have produced.
        var idString = id.StripFileLocalType( out var fileLocalTypeMetadataName ).Id;

        var indexOfAt = idString.IndexOfOrdinal( ';' );

        if ( indexOfAt > 0 )
        {
            // We have a parameter or a type parameter.

            var parts = idString.Split( _separators );

            var parentId = parts[0];
            var kind = parts[1];
            var ordinal = parts.Length == 3 ? int.Parse( parts[2], CultureInfo.InvariantCulture ) : -1;

            var parent = GetFirstSymbolForDeclarationId( parentId, compilation, fileLocalTypeMetadataName );

            if ( kind == nameof(RefTargetKind.Return) )
            {
                isReturnParameter = true;

                return parent;
            }

            return (parent?.Kind, kind) switch
            {
                (null, _) => null,
                (SymbolKind.Method, "Parameter") when parent is IMethodSymbol method => method.Parameters[ordinal],
                (SymbolKind.Method, "TypeParameter") when parent is IMethodSymbol method => method.TypeParameters[ordinal],
                (SymbolKind.NamedType, "TypeParameter") when parent is INamedTypeSymbol type => type.TypeParameters[ordinal],
                (SymbolKind.Property, "Parameter") when parent is IPropertySymbol property => property.Parameters[ordinal],
                (SymbolKind.NamedType, nameof(RefTargetKind.PrimaryConstructor)) when parent is INamedTypeSymbol type =>
                    type.InstanceConstructors.FirstOrDefault( c => c.IsPrimaryConstructor() ),
                _ => null
            };
        }
        else if ( idString.StartsWith( _assemblyPrefix, StringComparison.OrdinalIgnoreCase ) )
        {
            if ( !AssemblyIdentity.TryParseDisplayName( idString.Substring( _assemblyPrefix.Length ), out var assemblyIdentity ) )
            {
                throw new AssertionFailedException( $"Cannot parse the id '{id.Id}'." );
            }

            if ( compilation.Assembly.Identity.Equals( assemblyIdentity ) )
            {
                return compilation.Assembly;
            }
            else
            {
                return compilation.SourceModule.ReferencedAssemblySymbols.SingleOrDefault( a => a.Identity.Equals( assemblyIdentity ) );
            }
        }
        else
        {
            // Special case for the global namespace that's not handled by GetFirstSymbolForDeclarationId, see https://github.com/dotnet/roslyn/issues/66976.
            if ( idString == "N:" )
            {
                return compilation.Assembly.GlobalNamespace;
            }
            else if ( idString.StartsWith( SerializableTypeId.Prefix, StringComparison.Ordinal ) )
            {
                if ( !compilationContext.SerializableTypeIdResolver.TryResolveId( new SerializableTypeId( idString ), out var typeSymbol ) )
                {
                    return null;
                }
                else
                {
                    // Make sure to return the non-nullable type.
                    return typeSymbol.WithNullableAnnotation( NullableAnnotation.NotAnnotated );
                }
            }

            var symbol = GetFirstSymbolForDeclarationId( idString, compilation, fileLocalTypeMetadataName );

            // Make sure to return the non-nullable type.
            if ( symbol?.Kind is SymbolKind.NamedType or SymbolKind.ArrayType or SymbolKind.PointerType or SymbolKind.FunctionPointerType
                     or SymbolKind.DynamicType or SymbolKind.ErrorType or SymbolKind.TypeParameter
                 && symbol is ITypeSymbol { NullableAnnotation: NullableAnnotation.None } typeSymbol2 )
            {
                return typeSymbol2.WithNullableAnnotation( NullableAnnotation.NotAnnotated );
            }
            else
            {
                return symbol;
            }
        }
    }

    /// <summary>
    /// Returns the first symbol that matches the given documentation comment identifier and belongs to the file-local
    /// type that the identifier named, or <c>null</c> when there is none.
    /// </summary>
    /// <remarks>
    /// A documentation comment identifier cannot express a file-local type, so the compilation can contain several
    /// symbols that match it. All of them are enumerated and the discriminator selects among them.
    /// </remarks>
    private static ISymbol? GetFirstSymbolForDeclarationId( string documentationId, Compilation compilation, string? fileLocalTypeMetadataName )
    {
        foreach ( var candidate in DocumentationCommentId.GetSymbolsForDeclarationId( documentationId, compilation ) )
        {
            if ( MatchesFileLocalType( candidate, fileLocalTypeMetadataName ) )
            {
                return candidate;
            }
        }

        return null;
    }
}