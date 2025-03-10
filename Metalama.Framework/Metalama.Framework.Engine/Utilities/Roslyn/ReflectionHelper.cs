// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Utilities.Caching;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace Metalama.Framework.Engine.Utilities.Roslyn
{
    public static class ReflectionHelper
    {
        // From internal System.TypeNameKind in System.Private.CoreLib
        private enum TypeNameKind
        {
            Name,
            ToString,
            FullName
        }

        private static readonly WeakCache<INamespaceOrTypeSymbol, string> _reflectionNameCache = new();
        private static readonly WeakCache<INamespaceOrTypeSymbol, string> _reflectionFullNameCache = new();
        private static readonly WeakCache<INamespaceOrTypeSymbol, string> _reflectionToStringNameCache = new();

        internal static AssemblyIdentity ToAssemblyIdentity( this AssemblyName assemblyName )
        {
            ImmutableArray<byte> publicKeyOrToken;
            var hasPublicKey = false;

            if ( assemblyName.GetPublicKey() is { Length: > 0 } publicKey )
            {
                publicKeyOrToken = publicKey.ToImmutableArray();
                hasPublicKey = true;
            }
            else if ( assemblyName.GetPublicKeyToken() is { Length: > 0 } publicKeyToken )
            {
                publicKeyOrToken = publicKeyToken.ToImmutableArray();
            }
            else
            {
                publicKeyOrToken = default;
            }

            return new AssemblyIdentity(
                assemblyName.Name,
                assemblyName.Version,
                assemblyName.CultureName,
                publicKeyOrToken,
                hasPublicKey );
        }

        internal static INamedTypeSymbol GetTypeByMetadataNameSafe( this Compilation compilation, string name )
            => compilation.GetTypeByMetadataName( name ) ?? throw new ArgumentOutOfRangeException(
                nameof(name),
                $"Cannot find a type '{name}' in compilation '{compilation.AssemblyName}" );

        internal static IAssemblySymbol? GetAssembly( this Compilation compilation, string assemblyName )
        {
            if ( compilation.Assembly.Name == assemblyName )
            {
                return compilation.Assembly;
            }
            else
            {
                return compilation.SourceModule.ReferencedAssemblySymbols.FirstOrDefault( a => a.Name == assemblyName );
            }
        }

        /// <summary>
        /// Gets a string that would be equal to <see cref="MemberInfo.Name"/>.
        /// </summary>
        internal static string GetReflectionName( this INamespaceOrTypeSymbol s )
            => _reflectionNameCache.GetOrAdd( s, x => x.GetReflectionName( TypeNameKind.Name ) );

        /// <summary>
        /// Gets a string that would be equal to <see cref="Type.FullName"/>, except that we do not qualify type names with the assembly name.
        /// </summary>
        public static string GetReflectionFullName( this INamespaceOrTypeSymbol s )
            => _reflectionFullNameCache.GetOrAdd( s, x => x.GetReflectionName( TypeNameKind.FullName ) );

        /// <summary>
        /// Gets a string that would be equal to the returned value of <see cref="Type.ToString"/> method.
        /// </summary>
        internal static string GetReflectionToStringName( this INamespaceOrTypeSymbol s )
            => _reflectionToStringNameCache.GetOrAdd( s, x => x.GetReflectionName( TypeNameKind.ToString ) );

        private static string GetReflectionName( this INamespaceOrTypeSymbol s, TypeNameKind kind )
        {
            if ( s is ITypeParameterSymbol typeParameter )
            {
                return typeParameter.Name;
            }

            using var stringBuilderHandle = StringBuilderPool.Default.Allocate();

            var sb = stringBuilderHandle.Value;

            Append( s );

            return sb.ToString();

            void Append( INamespaceOrTypeSymbol symbol, List<ITypeSymbol>? typeArguments = null )
            {
                var currentTypeArguments = typeArguments ?? new List<ITypeSymbol>();

                // Append the containing namespace or type.
                if ( kind != TypeNameKind.Name && symbol is not ITypeParameterSymbol )
                {
                    switch ( symbol.ContainingSymbol )
                    {
                        case null:
                            break;

                        case ITypeSymbol type:
                            Append( type, currentTypeArguments );

                            sb.Append( '+' );

                            break;

                        case INamespaceSymbol ns:
                            if ( !ns.IsGlobalNamespace )
                            {
                                Append( ns );

                                sb.Append( '.' );
                            }

                            break;

                        default:
                            // A type is always contained in another type or in a namespace, possibly the global namespace.
                            throw new AssertionFailedException( $"'{symbol}' has an unexpected containing symbol kind {symbol.ContainingSymbol.Kind}." );
                    }
                }

                switch ( symbol )
                {
                    case INamedTypeSymbol { IsGenericType: true } unboundGenericType
                        when (!unboundGenericType.IsGenericTypeDefinition() && kind != TypeNameKind.Name)
                             || kind == TypeNameKind.ToString:
                        sb.Append( unboundGenericType.MetadataName );

                        currentTypeArguments.AddRange( unboundGenericType.TypeArguments );

                        break;

                    case { } when !string.IsNullOrEmpty( symbol.MetadataName ):
                        sb.Append( symbol.MetadataName );

                        break;

                    case IArrayTypeSymbol array:
                        Append( array.ElementType );

                        sb.Append( '[' );

                        for ( var i = 1; i < array.Rank; i++ )
                        {
                            sb.Append( ',' );
                        }

                        sb.Append( ']' );

                        break;

                    case IPointerTypeSymbol pointer:
                        Append( pointer.PointedAtType );

                        sb.Append( '*' );

                        break;

                    case IDynamicTypeSymbol:
                        sb.Append( "System.Object" );

                        break;

                    case IErrorTypeSymbol errorTypeSymbol:
                        // We try to write a name for an unresolved type, even if it is incorrect.
                        // If the caller requires a valid name, it has to verify the type validity differently.
                        sb.Append( errorTypeSymbol.Name );

                        break;

                    default:
                        throw new AssertionFailedException( $"Don't know how to process a {symbol!.Kind}." );
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

                        Append( arg );
                    }

                    sb.Append( ']' );
                }
            }
        }

        /// <summary>
        /// Gets a properly-escaped assembly-qualified type name from its components.
        /// </summary>
        /// <param name="typeName">The type name.</param>
        /// <param name="assemblyName">The assembly name.</param>
        /// <returns>A string of the form <c>TypeName, AssemblyName</c>, where commas in <paramref name="typeName"/> have been properly escaped.</returns>
        internal static string GetAssemblyQualifiedTypeName( string typeName, string assemblyName )
        {
            if ( typeName == null )
            {
                throw new ArgumentNullException( nameof(typeName) );
            }

            if ( assemblyName == null )
            {
                throw new ArgumentNullException( nameof(assemblyName) );
            }

            return typeName.ReplaceOrdinal( ",", "\\," ) + ", " + assemblyName;
        }

        /// <summary>
        /// Returns any method (static, instance, any accessibility) from the given type or its base types.
        /// </summary>
        internal static MethodInfo? GetAnyMethod( this Type type, string name )
            => type.GetMethod( name, BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic )
               ?? type.BaseType?.GetAnyMethod( name );
    }
}