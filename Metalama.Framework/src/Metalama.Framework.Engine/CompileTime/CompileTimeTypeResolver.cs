// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.ReflectionMocks;
using Metalama.Framework.Engine.CodeModel.Factories;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Caching;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Framework.Services;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Metalama.Framework.Engine.CompileTime;

/// <summary>
/// Provides the <see cref="GetCompileTimeType"/> method, which maps a Roslyn <see cref="ITypeSymbol"/> to a reflection
/// <see cref="Type"/>. The mapping is compilation-independent: the symbol carries everything needed, and the
/// implementations resolve through the current <c>AppDomain</c> and the project's <see cref="CompileTimeProjectRepository"/>.
/// Nothing here is scoped to a compilation: the concrete resolvers are plain <see cref="IProjectService"/>s. The caches are
/// keyed by <see cref="ITypeSymbol"/> and held weakly, so entries for a given compilation are collected along with it.
/// </summary>
/// <remarks>
/// This base class deliberately does <em>not</em> implement <see cref="IProjectService"/>; only the concrete resolvers do.
/// <see cref="ServiceProvider{TBase}"/> indexes a service under every base type that is assignable to the service interface,
/// so marking this class would index <see cref="SystemTypeResolver"/> and <see cref="ProjectSpecificCompileTimeTypeResolver"/>
/// under it and make the two conflict.
/// </remarks>
internal abstract class CompileTimeTypeResolver
{
    // Only used to produce a mock for symbols that cannot be mapped to a real, loadable Type. The factory does not depend
    // on any compilation; it is a cache whose lifetime is scoped to the project.
    private readonly CompileTimeTypeFactory _compileTimeTypeFactory;

    protected CompileTimeTypeResolver( CompileTimeTypeFactory compileTimeTypeFactory )
    {
        this._compileTimeTypeFactory = compileTimeTypeFactory;
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
            return this._compileTimeTypeFactory.Get( typeSymbol );
        }
        else
        {
            return type;
        }
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