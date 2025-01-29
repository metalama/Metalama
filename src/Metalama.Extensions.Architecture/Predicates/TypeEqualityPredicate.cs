// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Extensions.Validation;
using Metalama.Framework.Code;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Extensions.Architecture.Predicates;

internal class TypeEqualityPredicate : ReferenceEndPredicate
{
    private readonly IRef<INamedType>[] _typeRefs;

    public TypeEqualityPredicate( IEnumerable<Type> types, ReferencePredicateBuilder builder )
        : base( builder )
    {
        var iTypes = types.Select( TypeFactory.GetType );
        var typeRefs = new List<IRef<INamedType>>();

        foreach ( var iType in iTypes )
        {
            if ( iType is not INamedType namedType )
            {
                throw new InvalidOperationException(
                    $"The type '{iType}' cannot be used as a referencing type predicate parameter. Arrays, type parameters and pointers are not allowed." );
            }

            if ( namedType is { IsGeneric: true, IsCanonicalGenericInstance: false } )
            {
                throw new InvalidOperationException(
                    $"The type '{iType}' cannot be used as a referencing type predicate parameter. Bound generic types are not allowed." );
            }

            typeRefs.Add( namedType.Definition.ToRef() );
        }

        this._typeRefs = typeRefs.ToArray();
    }

    public TypeEqualityPredicate( IEnumerable<INamedType> types, ReferencePredicateBuilder builder )
        : base( builder )
    {
        var typeRefs = new List<IRef<INamedType>>();

        foreach ( var type in types )
        {
            if ( type is { IsGeneric: true, IsCanonicalGenericInstance: false } )
            {
                throw new InvalidOperationException(
                    $"The type '{type}' cannot be used as a referencing type predicate parameter. Bound generic types are not allowed." );
            }

            typeRefs.Add( type.Definition.ToRef() );
        }

        this._typeRefs = typeRefs.ToArray();
    }

    public override bool IsMatch( ReferenceEnd referenceEnd )
    {
        var referenceEndType = referenceEnd.Type;

        return this._typeRefs.Any( t => referenceEndType.Equals( t.GetTarget() ) );
    }

    protected override ReferenceGranularity GetGranularity() => ReferenceGranularity.Type;
}