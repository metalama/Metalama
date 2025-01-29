// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Extensions.Validation;

namespace Metalama.Extensions.Architecture.Predicates;

internal abstract class CombiningPredicateModifier : PredicateModifier
{
    protected ReferencePredicate OtherPredicate { get; }

    public CombiningPredicateModifier( ReferencePredicate otherPredicate )
    {
        this.OtherPredicate = otherPredicate;
    }

    public sealed override ReferenceGranularity ModifyGranularity( ReferenceGranularity baseGranularity )
        => baseGranularity.CombineWith( this.OtherPredicate.Granularity );
}