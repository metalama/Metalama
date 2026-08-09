// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.Collections;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using SpecialType = Microsoft.CodeAnalysis.SpecialType;

namespace Metalama.Framework.Engine.SerializableIds;

// ReSharper disable once MemberCanBeInternal
public sealed class SerializableTypeIdResolverForIType : SerializableTypeIdResolver<IType, INamespaceOrNamedType>
{
    private readonly CompilationModel _compilation;

    internal SerializableTypeIdResolverForIType( CompilationModel compilation )
    {
#if DEBUG
        if ( compilation.Name == "empty" )
        {
            throw new AssertionFailedException( "Expected a non-empty assembly." );
        }
#endif
        this._compilation = compilation;
    }

    protected override IReadOnlyDictionary<string, IType?> GetGenericContext( SerializableDeclarationId declarationId )
    {
        var declaration = (IGeneric) declarationId.ResolveToDeclaration( this._compilation ).AssertNotNull();
        var genericParameters = new Dictionary<string, IType?>();

        for ( var d = declaration; d != null; d = d.ContainingDeclaration as IGeneric )
        {
            foreach ( var typeParameter in d.TypeParameters )
            {
                genericParameters[typeParameter.Name] = typeParameter;
            }
        }

        return genericParameters;
    }

    protected override IType CreateArrayType( IType elementType, int rank, bool isNullOblivious )
    {
        var arrayType = elementType.MakeArrayType( rank );

        if ( isNullOblivious )
        {
            return arrayType.StripNullabilityAnnotation();
        }
        else if ( arrayType.IsNullable != false )
        {
            throw new AssertionFailedException();
        }

        return arrayType;
    }

    protected override IType CreatePointerType( IType pointedAtType ) => pointedAtType.MakePointerType();

    protected override IType CreateNullableType( IType elementType ) => elementType.ToNullable();

    protected override IType AddNonNullableAnnotation( IType referenceType )
    {
        // A type parameter is annotated rather than stripped. IType.ToNonNullable removes the annotation of a type
        // parameter, because C# cannot state that a type parameter is non-nullable and because the code model answers
        // the same nullability for the declaration of a parameter and for a use of it, so it cannot tell them apart.
        // A type parameter appearing as the type argument of a type annotated as non-nullable is nonetheless annotated
        // in the source the identifier was built from, and removing the annotation made the resolved type differ from
        // it, where the resolver of symbols reproduced it. The identifier of a type parameter that is itself the
        // outermost type carries no marker, so this method is never asked to annotate one. See issue #1839.
        if ( referenceType is ITypeParameter and ISymbolBasedCompilationElement symbolBasedType )
        {
            var typeSymbol = (ITypeSymbol) symbolBasedType.Symbol;

            return typeSymbol.NullableAnnotation == NullableAnnotation.NotAnnotated
                ? referenceType
                : this._compilation.Factory.GetIType(
                    typeSymbol.WithNullableAnnotation( NullableAnnotation.NotAnnotated ),
                    defaultNullability: null );
        }

        return referenceType.ToNonNullable();
    }

    protected override IType AddObliviousAnnotation( IType type )
        => type.IsReferenceType != false ? type.StripNullabilityAnnotation() : type;

    protected override IType ConstructGenericType( IType genericType, IType[] typeArguments )
        => genericType.AssertCast<INamedType>().WithTypeArguments( typeArguments );

    protected override IType CreateTupleType( ImmutableArray<IType> elementTypes, ImmutableArray<string?> elementNames )
    {
        Invariant.Assert( elementTypes.Length >= 2 );

        // The construction is delegated to the factory rather than repeated here. Looking System.ValueTuple up by the
        // arity of the tuple is correct only up to seven elements: the type is declared for the arities one to eight,
        // and its eighth type parameter holds the remaining elements and must itself be a tuple, which the factory
        // nests correctly. See issues #1841 and #1842.
        if ( elementNames.All( n => n == null ) )
        {
            return this._compilation.Factory.CreateTupleType( elementTypes );
        }

        // An element that is not named takes the default name of its position, which is what an unnamed element of a
        // tuple is called and is therefore not a name of its own.
        return this._compilation.Factory.CreateTupleType(
            elementTypes.Select( ( type, index ) => (Type: type, Name: elementNames[index] ?? $"Item{index + 1}") ) );
    }

    protected override IType DynamicType => this._compilation.Factory.GetIType( this._compilation.RoslynCompilation.DynamicType );

    protected override INamespaceOrNamedType? LookupName( string name, int arity, INamespaceOrNamedType? ns )
    {
        ns ??= this._compilation.GetMergedGlobalNamespace();

        var candidates = ns.DeclarationKind switch
        {
            DeclarationKind.Namespace when ns is INamespace iNamespace => iNamespace.Types.OfName( name )
                .ConcatNotNull<INamespaceOrNamedType>( iNamespace.Namespaces.OfName( name ) ),
            DeclarationKind.NamedType when ns is INamedType iNamedType => iNamedType.Types.OfName( name ),
            _ => throw new AssertionFailedException( $"Unexpected type {ns.GetType()}." )
        };

        foreach ( var member in candidates )
        {
            var memberArity = (member as INamedType)?.TypeParameters.Count ?? 0;

            if ( arity == memberArity )
            {
                return member;
            }
        }

        return null;
    }

    protected override IType GetSpecialType( SpecialType specialType ) => this._compilation.Factory.GetSpecialType( specialType.ToOurSpecialType() );

    protected override bool HasTypeParameterOfName( IType type, string name ) => type.AssertCast<INamedType>().TypeParameters.Any( t => t.Name == name );
}