// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Extensions.Validation;

namespace Metalama.Extensions.Architecture.Predicates;

internal sealed class OrPredicate : ReferencePredicate
{
    private readonly ReferencePredicate _predicate1;
    private readonly ReferencePredicate _predicate2;

    public OrPredicate( ReferencePredicate predicate1, ReferencePredicate predicate2 ) : base( predicate1.Builder )
    {
        this._predicate1 = predicate1;
        this._predicate2 = predicate2;
    }

    protected override bool IsMatchCore( ReferenceValidationContext context ) => this._predicate1.IsMatch( context ) || this._predicate2.IsMatch( context );

    protected override ReferenceGranularity GetGranularity() => this._predicate1.Granularity.CombineWith( this._predicate2.Granularity );
}