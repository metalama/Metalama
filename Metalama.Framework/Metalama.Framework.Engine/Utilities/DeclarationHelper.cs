// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Types;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Source;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Caching;
using Metalama.Framework.Engine.Utilities.Roslyn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Metalama.Framework.Engine.Utilities;

internal static class DeclarationHelper
{
    private enum TypeNameKind
    {
        Name,
        FullName
    }

    private static readonly WeakCache<IType, string> _reflectionNameCache = new();
    private static readonly WeakCache<IType, string> _reflectionFullNameCache = new();

    /// <summary>
    /// Gets a string that would be equal to <see cref="MemberInfo.Name"/>.
    /// </summary>
    /// <param name="bypassSymbols">Does not use the symbol-based implementation, even when available. Used for testing.</param>
    internal static string GetReflectionName( this IType type, bool bypassSymbols = false )
        => bypassSymbols
            ? type.GetReflectionName( TypeNameKind.Name, bypassSymbols: true )
            : _reflectionNameCache.GetOrAdd( type, x => x.GetReflectionName( TypeNameKind.Name ) );

    /// <summary>
    /// Gets a string that would be equal to <see cref="Type.FullName"/>, except that we do not qualify type names with the assembly name.
    /// </summary>
    /// <param name="bypassSymbols">Does not use the symbol-based implementation, even when available. Used for testing.</param>
    internal static string GetReflectionFullName( this IType type, bool bypassSymbols = false )
        => bypassSymbols
            ? type.GetReflectionName( TypeNameKind.FullName, bypassSymbols: true )
            : _reflectionFullNameCache.GetOrAdd( type, x => x.GetReflectionName( TypeNameKind.FullName ) );

    private static string GetReflectionName( this IType declaration, TypeNameKind kind, bool bypassSymbols = false )
    {
        if ( declaration.GetSymbol() is { } symbol && !bypassSymbols )
        {
            return kind switch
            {
                TypeNameKind.Name => symbol.GetReflectionName(),
                TypeNameKind.FullName => symbol.GetReflectionFullName(),
                _ => throw new AssertionFailedException( $"Unexpected TypeNameKind value: {kind}." ),
            };
        }

        if ( declaration is ITypeParameter typeParameter )
        {
            return typeParameter.Name;
        }

        var sb = new StringBuilder();

        Append( declaration, kind, sb );

        return sb.ToString();

        static void Append( ICompilationElement declaration, TypeNameKind kind, StringBuilder sb, List<IType>? typeArguments = null )
        {
            var currentTypeArguments = typeArguments ?? [];

            // Append the containing namespace or type.
            if ( kind != TypeNameKind.Name && declaration is not ITypeParameter )
            {
                switch ( (declaration as IDeclaration)?.GetContainingDeclarationOrNamespace() )
                {
                    case null:
                        break;

                    case IType type:
                        Append( type, kind, sb, currentTypeArguments );

                        sb.Append( '+' );

                        break;

                    case INamespace ns:
                        if ( !ns.IsGlobalNamespace )
                        {
                            Append( ns, kind, sb );

                            sb.Append( '.' );
                        }

                        break;

                    default:
                        // A type is always contained in another type or in a namespace, possibly the global namespace.
                        throw new AssertionFailedException( $"'{declaration}' has an unexpected containing declaration {(declaration as IDeclaration)?.GetContainingDeclarationOrNamespace()}." );
                }
            }

            switch ( declaration )
            {
                case INamedType { IsGeneric: true, IsCanonicalGenericInstance: false } unboundGenericType
                    when kind != TypeNameKind.Name:
                    sb.Append( unboundGenericType.GetMetadataName() );

                    currentTypeArguments.AddRange( unboundGenericType.TypeArguments );

                    break;

                case INamedDeclaration namedDeclaration:
                    sb.Append( namedDeclaration.GetMetadataName() );

                    break;

                case IArrayType array:
                    Append( array.ElementType, kind, sb );

                    sb.Append( '[' );

                    for ( var i = 1; i < array.Rank; i++ )
                    {
                        sb.Append( ',' );
                    }

                    sb.Append( ']' );

                    break;

                case IPointerType pointer:
                    Append( pointer.PointedAtType, kind, sb );

                    sb.Append( '*' );

                    break;

                case IDynamicType:
                    sb.Append( "System.Object" );

                    break;

                default:
                    throw new AssertionFailedException( $"Don't know how to process a {declaration.GetType()}." );
            }

            if ( typeArguments == null && currentTypeArguments.Any() )
            {
                sb.Append( '[' );

                for ( var i = 0; i < currentTypeArguments.Count; i++ )
                {
                    if ( i > 0 )
                    {
                        sb.Append( ',' );
                    }

                    var arg = currentTypeArguments[i];

                    Append( arg, kind, sb );
                }

                sb.Append( ']' );
            }
        }
    }

    private static string GetMetadataName( this INamedDeclaration declaration )
        => declaration is IGeneric { TypeArguments.Count: > 0 and var count }
            ? $"{declaration.Name}`{count}"
            : declaration.Name;

    internal static bool? BelongsToCompilation( this IType type, CompilationContext compilationContext )
    {
        var assembly = (type as IDeclaration)?.DeclaringAssembly;

        if ( assembly == null )
        {
            return null;
        }

        if ( !compilationContext.Assemblies.TryGetValue( ((AssemblyIdentityModel) assembly.Identity).Identity, out var thisCompilationAssembly ) )
        {
            // If we cannot find the assembly, we cannot make any decision whether this is or not a legit symbol.
            // It can happen that a referenced assembly has symbols to another referenced assembly that is not directly
            // referenced by our compilation.

            return null;
        }

        return assembly.GetSymbol().Equals( thisCompilationAssembly );
    }

    /// <summary>
    /// Get the merged global namespace, i.e. one that contains both declared and referenced types.
    /// </summary>
    public static INamespace GetMergedGlobalNamespace( this CompilationModel compilation )
        => compilation.Factory.GetNamespace( compilation.RoslynCompilation.GlobalNamespace );
}