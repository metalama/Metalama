// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Extensions.Validation;
using System.Collections.Immutable;

namespace Metalama.Extensions.Architecture.Predicates;

internal sealed class AnyPredicate : CombiningReferencePredicate
{
    public AnyPredicate( ImmutableArray<ReferencePredicate> predicates, ReferencePredicateBuilder builder ) : base( predicates, builder ) { }

    protected override bool IsMatchCore( ReferenceValidationContext context )
    {
        foreach ( var predicate in this.Predicates )
        {
            if ( predicate.IsMatch( context ) )
            {
                return true;
            }
        }

        return false;
    }
}