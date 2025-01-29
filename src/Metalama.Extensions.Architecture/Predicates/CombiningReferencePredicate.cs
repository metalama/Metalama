// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Extensions.Validation;
using System.Collections.Immutable;

namespace Metalama.Extensions.Architecture.Predicates;

internal abstract class CombiningReferencePredicate : ReferencePredicate
{
    protected ImmutableArray<ReferencePredicate> Predicates { get; }

    public CombiningReferencePredicate( ImmutableArray<ReferencePredicate> predicates, ReferencePredicateBuilder builder ) : base( builder )
    {
        this.Predicates = predicates;
    }

    protected override ReferenceGranularity GetGranularity()
    {
        var granularity = ReferenceGranularity.Compilation;

        foreach ( var predicate in this.Predicates )
        {
            granularity = granularity.CombineWith( predicate.Granularity );
        }

        return granularity;
    }
}