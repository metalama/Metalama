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
        var compilation = compilationContext.Compilation;

        isReturnParameter = false;

        // Handle file-local type discriminator: strip the |hash suffix before further processing.
        var (effectiveIdString, fileLocalHash) = ParseFileLocalSuffix( id.Id );

        var indexOfAt = effectiveIdString.IndexOfOrdinal( ';' );

        if ( indexOfAt > 0 )
        {
            // We have a parameter or a type parameter.

            var parts = effectiveIdString.Split( _separators );

            var parentId = parts[0];
            var kind = parts[1];
            var ordinal = parts.Length == 3 ? int.Parse( parts[2], CultureInfo.InvariantCulture ) : -1;

            var parent = ResolveSymbolForDocId( parentId, compilation, fileLocalHash );

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
        else if ( effectiveIdString.StartsWith( _assemblyPrefix, StringComparison.OrdinalIgnoreCase ) )
        {
            if ( !AssemblyIdentity.TryParseDisplayName( effectiveIdString.Substring( _assemblyPrefix.Length ), out var assemblyIdentity ) )
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
            if ( effectiveIdString == "N:" )
            {
                return compilation.Assembly.GlobalNamespace;
            }
            else if ( effectiveIdString.StartsWith( SerializableTypeId.Prefix, StringComparison.Ordinal ) )
            {
                if ( !compilationContext.SerializableTypeIdResolver.TryResolveId( new SerializableTypeId( effectiveIdString ), out var typeSymbol ) )
                {
                    return null;
                }
                else
                {
                    // Make sure to return the non-nullable type.
                    return typeSymbol.WithNullableAnnotation( NullableAnnotation.NotAnnotated );
                }
            }

            var symbol = ResolveSymbolForDocId( effectiveIdString, compilation, fileLocalHash );

            // Make sure to return the non-nullable type.
            if ( symbol?.Kind is SymbolKind.NamedType or SymbolKind.ArrayType or SymbolKind.PointerType or SymbolKind.FunctionPointerType or SymbolKind.DynamicType or SymbolKind.ErrorType or SymbolKind.TypeParameter
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
    /// Resolves a documentation comment ID to a symbol, optionally filtering by file-local hash for file-local types.
    /// </summary>
    private static ISymbol? ResolveSymbolForDocId( string docId, Compilation compilation, string? fileLocalHash )
    {
        if ( fileLocalHash == null )
        {
            return DocumentationCommentId.GetFirstSymbolForDeclarationId( docId, compilation );
        }

        // For file-local types, we may get multiple symbols with the same documentation comment ID.
        // We need to find the one with the matching hash.
        var symbols = DocumentationCommentId.GetSymbolsForDeclarationId( docId, compilation );

        foreach ( var candidate in symbols )
        {
            if ( GetFileLocalHash( candidate ) == fileLocalHash )
            {
                return candidate;
            }
        }

        // Fallback: return the first match if no file-local match found.
        return symbols.IsEmpty ? null : symbols[0];
    }
}