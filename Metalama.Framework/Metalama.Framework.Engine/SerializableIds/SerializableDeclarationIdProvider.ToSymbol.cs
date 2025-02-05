// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities;
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

        var indexOfAt = id.Id.IndexOfOrdinal( ';' );

        if ( indexOfAt > 0 )
        {
            // We have a parameter or a type parameter.

            var parts = id.Id.Split( _separators );

            var parentId = parts[0];
            var kind = parts[1];
            var ordinal = parts.Length == 3 ? int.Parse( parts[2], CultureInfo.InvariantCulture ) : -1;

            var parent = DocumentationCommentId.GetFirstSymbolForDeclarationId( parentId, compilation );

            if ( kind == nameof(RefTargetKind.Return) )
            {
                isReturnParameter = true;

                return parent;
            }

            return (parent, kind) switch
            {
                (null, _) => null,
                (IMethodSymbol method, "Parameter") => method.Parameters[ordinal],
                (IMethodSymbol method, "TypeParameter") => method.TypeParameters[ordinal],
                (INamedTypeSymbol type, "TypeParameter") => type.TypeParameters[ordinal],
                (IPropertySymbol property, "Parameter") => property.Parameters[ordinal],
                _ => null
            };
        }
        else if ( id.Id.StartsWith( _assemblyPrefix, StringComparison.OrdinalIgnoreCase ) )
        {
            if ( !AssemblyIdentity.TryParseDisplayName( id.Id.Substring( _assemblyPrefix.Length ), out var assemblyIdentity ) )
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
            if ( id.Id == "N:" )
            {
                return compilation.Assembly.GlobalNamespace;
            }
            else if ( id.Id.StartsWith( SerializableTypeId.Prefix, StringComparison.Ordinal ) )
            {
                if ( !compilationContext.SerializableTypeIdResolver.TryResolveId( new SerializableTypeId( id.Id ), out var typeSymbol ) )
                {
                    return null;
                }
                else
                {
                    return typeSymbol;
                }
            }

            return DocumentationCommentId.GetFirstSymbolForDeclarationId( id.ToString(), compilation );
        }
    }
}