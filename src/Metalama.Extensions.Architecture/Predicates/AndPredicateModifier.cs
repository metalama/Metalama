// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Extensions.Validation;

namespace Metalama.Extensions.Architecture.Predicates;

internal class AndPredicateModifier : CombiningPredicateModifier
{
    public AndPredicateModifier( ReferencePredicate otherPredicate ) : base( otherPredicate ) { }

    public override bool IsMatch( bool currentPredicateResult, ReferenceValidationContext context )
        => currentPredicateResult && this.OtherPredicate.IsMatch( context );
}