// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using SpecialType = Microsoft.CodeAnalysis.SpecialType;

namespace Metalama.Framework.Engine.SerializableIds;

public sealed class SerializableTypeIdResolverForSymbol : SerializableTypeIdResolver<ITypeSymbol, INamespaceOrTypeSymbol>
{
    private readonly CompilationContext _compilationContext;

    private Compilation Compilation => this._compilationContext.Compilation;

    internal SerializableTypeIdResolverForSymbol( CompilationContext compilation )
    {
#if DEBUG
        if ( compilation.Compilation.AssemblyName == "empty" )
        {
            throw new AssertionFailedException( "Expected a non-empty assembly." );
        }
#endif
        this._compilationContext = compilation;
    }

    // ReSharper disable once MemberCanBeInternal
    public ITypeSymbol ResolveId( SerializableTypeId typeId, IReadOnlyDictionary<string, IType>? genericArguments = null )
    {
        var genericArgumentSymbols = genericArguments?.ToDictionary( kv => kv.Key, kv => kv.Value.GetSymbol() );

        return this.ResolveId( typeId, genericArgumentSymbols! );
    }

    protected override IReadOnlyDictionary<string, ITypeSymbol?> GetGenericContext( SerializableDeclarationId declarationId )
    {
        var declaration = declarationId.ResolveToSymbol( this._compilationContext );

        var dictionary = new Dictionary<string, ITypeSymbol?>();

        for ( var d = declaration; d != null; d = d.ContainingSymbol )
        {
            var typeParameters
                = d switch
                {
                    IMethodSymbol methodSymbol => methodSymbol.TypeArguments,
                    INamedTypeSymbol namedTypeSymbol => namedTypeSymbol.TypeArguments,
                    _ => default
                };

            if ( typeParameters.IsDefault )
            {
                break;
            }

            foreach ( var typeParameter in typeParameters )
            {
                dictionary[typeParameter.Name] = typeParameter;
            }
        }

        return dictionary;
    }

    protected override ITypeSymbol CreateArrayType( ITypeSymbol elementType, int rank ) => this.Compilation.CreateArrayTypeSymbol( elementType, rank );

    protected override ITypeSymbol CreatePointerType( ITypeSymbol pointedAtType ) => this.Compilation.CreatePointerTypeSymbol( pointedAtType );

    protected override ITypeSymbol CreateNullableType( ITypeSymbol elementType )
        => elementType.IsValueType
            ? this.Compilation.GetSpecialType( SpecialType.System_Nullable_T ).Construct( elementType )
            : elementType.WithNullableAnnotation( NullableAnnotation.Annotated );

    protected override ITypeSymbol CreateNonNullableReferenceType( ITypeSymbol referenceType )
        => referenceType.WithNullableAnnotation( NullableAnnotation.NotAnnotated );

    protected override ITypeSymbol ConstructGenericType( ITypeSymbol genericType, ITypeSymbol[] typeArguments )
        => genericType.AssertCast<INamedTypeSymbol>().Construct( typeArguments );

    protected override ITypeSymbol CreateTupleType( ImmutableArray<ITypeSymbol> elementTypes ) => this.Compilation.CreateTupleTypeSymbol( elementTypes );

    protected override ITypeSymbol DynamicType => this.Compilation.DynamicType;

    protected override INamespaceOrTypeSymbol? LookupName( string name, int arity, INamespaceOrTypeSymbol? ns )
    {
        ns ??= this.Compilation.GlobalNamespace;

        var candidates = ns.GetMembers( name );

        foreach ( var member in candidates )
        {
            var memberArity = member.Kind == SymbolKind.Namespace ? 0 : ((INamedTypeSymbol) member).Arity;

            if ( arity == memberArity )
            {
                return (INamespaceOrTypeSymbol) member;
            }
        }

        return null;
    }

    protected override ITypeSymbol GetSpecialType( SpecialType specialType ) => this.Compilation.GetSpecialType( specialType );

    protected override bool HasTypeParameterOfName( ITypeSymbol type, string name )
        => type.AssertCast<INamedTypeSymbol>().TypeParameters.Any( t => t.Name == name );
}