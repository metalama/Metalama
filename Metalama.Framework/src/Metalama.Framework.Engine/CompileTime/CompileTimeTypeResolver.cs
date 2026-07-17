// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.ReflectionMocks;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Caching;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Metalama.Framework.Engine.CompileTime;

/// <summary>
/// Provides the <see cref="GetCompileTimeType(ITypeSymbol, bool, CancellationToken)"/> method, which maps a Roslyn <see cref="ITypeSymbol"/>
/// to a reflection <see cref="Type"/>.
/// </summary>
internal abstract class CompileTimeTypeResolver : ICompilationService
{
    private readonly CompilationContext _compilationContext;

    protected CompileTimeTypeResolver( CompilationContext compilationContext )
    {
        this._compilationContext = compilationContext;
    }

    protected WeakCache<ITypeSymbol, Type?> Cache { get; } = new();

    /// <summary>
    /// Maps a Roslyn <see cref="!:ITypeSymbol" /> to a reflection <see cref="!:Type" />. 
    /// </summary>
    protected abstract Type? GetCompileTimeNamedType( INamedTypeSymbol typeSymbol, CancellationToken cancellationToken = default );

    public Type? GetCompileTimeType(
        ITypeSymbol typeSymbol,
        bool fallbackToMock,
        CancellationToken cancellationToken = default )
    {
        var type = this.Cache.GetOrAdd( typeSymbol, this.GetCompileTimeTypeCore, cancellationToken );

        if ( type == null && fallbackToMock )
        {
            return this._compilationContext.CompileTimeTypeFactory.Get( typeSymbol );
        }
        else
        {
            return type;
        }
    }

    /// <summary>
    /// Finds a named type by its metadata name in a given assembly of the compilation, and maps it to a reflection <see cref="Type"/>
    /// (falling back to a mock <see cref="CompileTimeType"/> if it cannot be mapped to a real, loadable <see cref="Type"/>).
    /// </summary>
    public Type GetCompileTimeType( string typeName, string assemblyName )
    {
        var assemblySymbol = this._compilationContext.Compilation.GetAssembly( assemblyName )
                             ?? throw new InvalidOperationException( $"Could not locate assembly {assemblyName} in compilation." );

        var typeSymbol = assemblySymbol.GetTypeByMetadataName( typeName )
                         ?? throw new InvalidOperationException( $"Could not locate type {typeName} in assembly {assemblyName} in compilation." );

        return this.GetCompileTimeType( typeSymbol, true ).AssertNotNull();
    }

    /// <summary>
    /// Constructs the reflection <see cref="Type"/> (or mock <see cref="CompileTimeType"/>) of an array of a given element type and rank.
    /// </summary>
    public Type GetCompileTimeArrayType( CompileTimeType elementType, int rank )
    {
        var compilation = this._compilationContext.Compilation;
        var elementTypeSymbol = (INamedTypeSymbol) elementType.Target.GetSymbol( compilation ).AssertNotNull();
        var arrayTypeSymbol = compilation.CreateArrayTypeSymbol( elementTypeSymbol, rank );

        return this.GetCompileTimeType( arrayTypeSymbol, true ).AssertNotNull();
    }

    /// <summary>
    /// Constructs the reflection <see cref="Type"/> (or mock <see cref="CompileTimeType"/>) of a generic type given its (open)
    /// generic type definition and a set of type arguments.
    /// </summary>
    public Type GetCompileTimeGenericType( Type genericTypeDefinition, Type[] genericArguments )
    {
        var mapper = this._compilationContext.ReflectionMapper;
        var genericTypeSymbol = (INamedTypeSymbol) mapper.GetTypeSymbol( genericTypeDefinition );
        var constructedTypeSymbol = genericTypeSymbol.Construct( genericArguments.SelectAsArray( mapper.GetTypeSymbol ) );

        return this.GetCompileTimeType( constructedTypeSymbol, true ).AssertNotNull();
    }

    private Type? GetCompileTimeTypeCore( ITypeSymbol typeSymbol, CancellationToken cancellationToken = default )
    {
        switch ( typeSymbol.Kind )
        {
            case SymbolKind.ArrayType when typeSymbol is IArrayTypeSymbol arrayType:
                {
                    var elementType = this.GetCompileTimeType( arrayType.ElementType, false, cancellationToken );

                    if ( elementType == null )
                    {
                        return null;
                    }

                    if ( arrayType.IsSZArray )
                    {
                        return elementType.MakeArrayType();
                    }
                    else
                    {
                        return elementType.MakeArrayType( arrayType.Rank );
                    }
                }

            case SymbolKind.NamedType when typeSymbol is INamedTypeSymbol { IsGenericType: true } genericType && !genericType.IsGenericTypeDefinition():
                {
                    var typeDefinition = this.GetCompileTimeNamedType( genericType.OriginalDefinition );

                    if ( typeDefinition == null )
                    {
                        return null;
                    }

                    var typeArguments = CollectTypeArguments( genericType )
                        .Select( arg => this.GetCompileTimeType( arg, false, cancellationToken ) )
                        .ToArray();

                    if ( typeArguments.Contains( null ) )
                    {
                        return null;
                    }

                    return typeDefinition.MakeGenericType( typeArguments.AssertNoneNull() );
                }

            case SymbolKind.NamedType when typeSymbol is INamedTypeSymbol namedType:
                return this.GetCompileTimeNamedType( namedType, cancellationToken ) ?? null;

            case SymbolKind.DynamicType:
                return typeof(object);

            case SymbolKind.PointerType when typeSymbol is IPointerTypeSymbol pointerType:
                {
                    var elementType = this.GetCompileTimeType( pointerType.PointedAtType, false, cancellationToken );

                    if ( elementType == null )
                    {
                        return null;
                    }
                    else
                    {
                        return elementType.MakePointerType();
                    }
                }

            case SymbolKind.TypeParameter:
                {
                    // It would be complex to properly map a type parameter, so we will use a mock. It works in most cases, and if we
                    // need it (because of type equality issues), we can implement the logic.
                    return null;
                }

            default:
                throw new AssertionFailedException( $"Don't know how to map the '{typeSymbol}' type." );
        }

        static IEnumerable<ITypeSymbol> CollectTypeArguments( INamedTypeSymbol? s )
        {
            var typeArguments = new List<ITypeSymbol>();

            while ( s != null )
            {
                typeArguments.InsertRange( 0, s.TypeArguments );

                s = s.ContainingSymbol as INamedTypeSymbol;
            }

            return typeArguments;
        }
    }
}