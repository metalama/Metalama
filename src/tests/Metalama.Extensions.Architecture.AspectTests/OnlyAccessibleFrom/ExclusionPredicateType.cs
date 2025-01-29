// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Extensions.Architecture.Aspects;
using Metalama.Extensions.Architecture.Predicates;
using Metalama.Extensions.Validation;

namespace Metalama.Extensions.Architecture.AspectTests.OnlyAccessibleFrom.ExclusionPredicateType
{
    [CanOnlyBeUsedFrom( Types = new[] { typeof(AllowedClass) }, ExclusionPredicateType = typeof(ExcludeNestedTypesPredicate) )]
    internal class ConstrainedClass { }

    internal class Forbidden : ConstrainedClass
    {
        private class AllowedBecauseNested : ConstrainedClass { }
    }

    internal class AllowedClass : ConstrainedClass { }

    internal class ExcludeNestedTypesPredicate : ReferencePredicate
    {
        public ExcludeNestedTypesPredicate( ReferencePredicateBuilder builder ) : base( builder ) { }

        protected override bool IsMatchCore( ReferenceValidationContext context ) => context.Origin.Type.DeclaringType != null;

        protected override ReferenceGranularity GetGranularity() => ReferenceGranularity.Type;
    }
}