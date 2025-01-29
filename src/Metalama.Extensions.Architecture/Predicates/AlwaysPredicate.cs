// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Extensions.Validation;

namespace Metalama.Extensions.Architecture.Predicates;

internal class AlwaysPredicate : ReferencePredicate
{
    public AlwaysPredicate( ReferencePredicateBuilder builder ) : base( builder ) { }

    protected override bool IsMatchCore( ReferenceValidationContext context ) => true;

    protected override ReferenceGranularity GetGranularity() => ReferenceGranularity.Compilation;
}