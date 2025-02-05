// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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
/// Provides the <see cref="GetCompileTimeType"/> method, which maps a Roslyn <see cref="ITypeSymbol"/> to a reflection <see cref="Type"/>.
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

    private Type? GetCompileTimeTypeCore( ITypeSymbol typeSymbol, CancellationToken cancellationToken = default )
    {
        switch ( typeSymbol )
        {
            case IArrayTypeSymbol arrayType:
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

            case INamedTypeSymbol { IsGenericType: true } genericType when !genericType.IsGenericTypeDefinition():
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

            case INamedTypeSymbol namedType:
                return this.GetCompileTimeNamedType( namedType, cancellationToken ) ?? null;

            case IDynamicTypeSymbol:
                return typeof(object);

            case IPointerTypeSymbol pointerType:
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

            case ITypeParameterSymbol:
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