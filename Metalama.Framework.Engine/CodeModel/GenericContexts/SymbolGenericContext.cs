// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using TypeParameterKind = Microsoft.CodeAnalysis.TypeParameterKind;

namespace Metalama.Framework.Engine.CodeModel.GenericContexts;

/// <summary>
/// Implements a <see cref="GenericContext"/> where all type parameters are mapped to symbols.
/// </summary>
internal sealed partial class SymbolGenericContext : GenericContext
{
    private readonly CompilationContext _compilationContext;

    public INamedTypeSymbol NamedTypeSymbol { get; }

    private readonly IMethodSymbol? _methodSymbol;

    private SymbolGenericContext( INamedTypeSymbol namedTypeSymbol, IMethodSymbol? methodSymbol, CompilationContext compilationContext )
    {
        // Assert that we only create a non-empty GenericContext only when we have a non-canonical mapping.
        Invariant.Assert( !namedTypeSymbol.IsDefinitionSafe() || methodSymbol != null );
        Invariant.Assert( methodSymbol == null || !methodSymbol.IsDefinitionSafe() );

        this.NamedTypeSymbol = namedTypeSymbol;
        this._methodSymbol = methodSymbol;
        this._compilationContext = compilationContext;
    }

    public static GenericContext Get( INamedTypeSymbol? symbol, CompilationContext compilationContext )
    {
        if ( symbol == null || symbol.IsDefinitionSafe() )
        {
            return Empty;
        }

        return new SymbolGenericContext( symbol, null, compilationContext );
    }

    public static GenericContext Get( IMethodSymbol? symbol, CompilationContext compilationContext )
    {
        if ( symbol == null || symbol.IsDefinitionSafe() )
        {
            return Empty;
        }

        var genericMethodSymbol = symbol.TypeArguments.IsEmpty ? null : symbol;

        return new SymbolGenericContext( symbol.ContainingType, genericMethodSymbol, compilationContext );
    }

    public static GenericContext Get( ISymbol? symbol, CompilationContext compilationContext )
    {
        var closestMember = symbol?.GetClosestContainingMember();

        if ( closestMember == null )
        {
            return Empty;
        }

        return closestMember.Kind switch
        {
            SymbolKind.Method => Get( (IMethodSymbol) closestMember, compilationContext ),
            SymbolKind.NamedType => Get( (INamedTypeSymbol) closestMember, compilationContext ),
            _ => Get( closestMember.ContainingType, compilationContext )
        };
    }

    [Memo]
    private TypeSymbolMapper TypeSymbolMapperInstance => new( this );

    [Memo]
    private SymbolMapper SymbolMapperInstance => new( this );

    private ITypeSymbol MapToSymbol( ITypeParameterSymbol typeParameter )
    {
        if ( this.IsEmptyOrIdentity )
        {
            return typeParameter;
        }

        switch ( typeParameter.TypeParameterKind )
        {
            case TypeParameterKind.Type:
                {
                    // Find which type of the stack of nested types we have to consider.
                    var requestedTypeDefinition = typeParameter.DeclaringType!.OriginalDefinition;

                    for ( var type = this.NamedTypeSymbol; type != null; type = type.ContainingType )
                    {
                        if ( type.OriginalDefinition == requestedTypeDefinition )
                        {
                            return type.TypeArguments[typeParameter.Ordinal];
                        }
                    }

                    // The type parameter cannot be matched. This can happen when we are trying to match a nested type A<T1>.B<T2> in the context of A<string>,
                    // i.e. the top-level type is bound and the nested type is unbound.
                    return typeParameter;
                }

            case TypeParameterKind.Method when this._methodSymbol == null:
                // Cannot map it.
                return typeParameter;

            case TypeParameterKind.Method:
                return this._methodSymbol.TypeArguments[typeParameter.Ordinal];

            default:
                throw new AssertionFailedException();
        }
    }

    [return: NotNullIfNotNull( nameof(type) )]
    public ITypeSymbol? MapToSymbol( ITypeSymbol? type )
    {
        if ( this.IsEmptyOrIdentity )
        {
            return type;
        }

        return type switch
        {
            null => null,
            ITypeParameterSymbol typeParameter => this.MapToSymbol( typeParameter ),
            _ => ReferencesTypeParameter( type ) ? this.TypeSymbolMapperInstance.Visit( type ) : type
        };
    }

    [return: NotNullIfNotNull( nameof(symbol) )]
    public ISymbol? MapToSymbol( ISymbol? symbol )
    {
        if ( this.IsEmptyOrIdentity )
        {
            return symbol;
        }

        if ( symbol == null )
        {
            return null;
        }

        return this.SymbolMapperInstance.Visit( symbol ).AssertSymbolNotNull();
    }

    internal override GenericContextKind Kind => GenericContextKind.Symbol;

    internal override ImmutableArray<IFullRef<IType>> TypeArguments => throw new NotImplementedException();

    internal override IType Map( ITypeParameter typeParameter )
    {
        if ( this.IsEmptyOrIdentity )
        {
            return typeParameter;
        }

        using ( StackOverflowHelper.Detect() )
        {
            switch ( typeParameter.TypeParameterKind )
            {
                case Code.TypeParameterKind.Type:
                    {
                        // Find which type of the stack of nested types we have to consider.
                        var requestedTypeDefinition = ((INamedType) typeParameter.ContainingDeclaration.AssertNotNull()).Definition.GetSymbol()
                            .AssertSymbolNullNotImplemented( "Generic context of constructed type." );

                        for ( var type = this.NamedTypeSymbol; type != null; type = type.ContainingType )
                        {
                            if ( type.OriginalDefinition == requestedTypeDefinition )
                            {
                                var typeArgument = type.TypeArguments[typeParameter.Index];

                                if ( typeArgument is ITypeParameterSymbol { TypeParameterKind: TypeParameterKind.Type } typeArgumentAsTypeParameter
                                     && typeArgumentAsTypeParameter.Ordinal == typeParameter.Index )
                                {
                                    // Avoid an infinite recursion trying to resolve it.
                                    return typeParameter;
                                }
                                else
                                {
                                    return typeParameter.GetCompilationModel().Factory.GetIType( typeArgument );
                                }
                            }
                        }

                        // The type parameter cannot be matched. This can happen when we are trying to match a nested type A<T1>.B<T2> in the context of A<string>,
                        // i.e. the top-level type is bound and the nested type is unbound.
                        return typeParameter;
                    }

                case Code.TypeParameterKind.Method when this._methodSymbol == null:
                    // Cannot map it.
                    return typeParameter;

                case Code.TypeParameterKind.Method:
                    {
                        var typeArgument = this._methodSymbol.TypeArguments[typeParameter.Index];

                        if ( typeArgument is ITypeParameterSymbol { TypeParameterKind: TypeParameterKind.Method } typeArgumentAsTypeParameter
                             && typeArgumentAsTypeParameter.Ordinal == typeParameter.Index )
                        {
                            // Avoid an infinite recursion trying to resolve it.
                            return typeParameter;
                        }
                        else
                        {
                            return typeParameter.GetCompilationModel().Factory.GetIType( typeArgument );
                        }
                    }

                default:
                    throw new AssertionFailedException();
            }
        }
    }

    protected override IType Map( ITypeParameterSymbol typeParameterSymbol, CompilationModel compilation )
    {
        return compilation.Factory.GetIType( this.MapToSymbol( typeParameterSymbol ) );
    }

    internal override GenericContext Map( GenericContext genericContext, RefFactory refFactory )
    {
        return MapRecursive( this._methodSymbol ?? (ISymbol) this.NamedTypeSymbol );

        IntroducedGenericContext MapRecursive( ISymbol symbol )
        {
            ImmutableArray<ITypeSymbol> typeArguments;
            IntroducedGenericContext? parentContext;
            ISymbol symbolDefinition;

            switch ( symbol )
            {
                case IMethodSymbol methodSymbol:
                    parentContext = MapRecursive( methodSymbol.ContainingSymbol );
                    typeArguments = methodSymbol.TypeArguments;
                    symbolDefinition = methodSymbol.OriginalDefinition;

                    break;

                case INamedTypeSymbol namedTypeSymbol:
                    parentContext = namedTypeSymbol.ContainingType != null ? MapRecursive( namedTypeSymbol.ContainingType ) : null;
                    typeArguments = namedTypeSymbol.TypeArguments;
                    symbolDefinition = namedTypeSymbol.OriginalDefinition;

                    break;

                default:
                    throw new AssertionFailedException();
            }

            if ( typeArguments.Length > 0 )
            {
                var mappedTypeArguments = ImmutableArray.CreateBuilder<IFullRef<IType>>( typeArguments.Length );

                foreach ( var typeArgumentSymbol in typeArguments )
                {
                    mappedTypeArguments.Add( genericContext.Map( typeArgumentSymbol, refFactory ) );
                }

                var mappedGenericContext = new IntroducedGenericContext(
                    mappedTypeArguments.MoveToImmutable(),
                    refFactory.FromAnySymbol( symbolDefinition ).As<IDeclaration>(),
                    parentContext );

                return mappedGenericContext;
            }
            else
            {
                return parentContext.AssertNotNull();
            }
        }
    }

    public override bool Equals( GenericContext? other )
    {
        if ( other is not SymbolGenericContext otherSymbolGenericContect )
        {
            return false;
        }

        return SymbolEqualityComparer.IncludeNullability.Equals( this.NamedTypeSymbol, otherSymbolGenericContect.NamedTypeSymbol );
    }

    protected override int GetHashCodeCore() => SymbolEqualityComparer.IncludeNullability.GetHashCode( this.NamedTypeSymbol );

    public override string ToString()
        => (this.NamedTypeSymbol, MethodSymbol: this._methodSymbol) switch
        {
            (_, not null) => $"SymbolGenericContext Method={{{this._methodSymbol.ToDebugString()}}}",
            _ => $"SymbolGenericContext Type={{{this.NamedTypeSymbol.ToDebugString()}}}"
        };
}