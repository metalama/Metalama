// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Collections;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Roslyn;
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

    protected override IType CreateArrayType( IType elementType, int rank ) => elementType.MakeArrayType( rank );

    protected override IType CreatePointerType( IType pointedAtType ) => pointedAtType.MakePointerType();

    protected override IType CreateNullableType( IType elementType ) => elementType.ToNullable();

    protected override IType CreateNonNullableReferenceType( IType referenceType ) => referenceType.ToNonNullable();

    protected override IType ConstructGenericType( IType genericType, IType[] typeArguments )
        => genericType.AssertCast<INamedType>().WithTypeArguments( typeArguments );

    protected override IType CreateTupleType( ImmutableArray<IType> elementTypes )
    {
        Invariant.Assert( elementTypes.Length >= 2 );

        var tupleType = this._compilation.Factory.GetTypeByReflectionName( $"System.ValueTuple`{elementTypes.Length}" );

        return tupleType.WithTypeArguments( elementTypes.ToArray() );
    }

    protected override IType DynamicType => this._compilation.Factory.GetIType( this._compilation.RoslynCompilation.DynamicType );

    protected override INamespaceOrNamedType? LookupName( string name, int arity, INamespaceOrNamedType? ns )
    {
        ns ??= this._compilation.GetMergedGlobalNamespace();

        var candidates = ns switch
        {
            INamespace iNamespace => iNamespace.Types.OfName( name ).ConcatNotNull<INamespaceOrNamedType>( iNamespace.Namespaces.OfName( name ) ),
            INamedType iNamedType => iNamedType.Types.OfName( name ),
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