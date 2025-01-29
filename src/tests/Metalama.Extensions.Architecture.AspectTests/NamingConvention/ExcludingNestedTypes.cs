// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Extensions.Architecture.Aspects;
using Metalama.Extensions.Architecture.Predicates;
using Metalama.Extensions.Validation;

namespace Metalama.Extensions.Architecture.AspectTests.NamingConvention.ExcludingNestedTypes
{
    [DerivedTypesMustRespectNamingConvention( "*Factory", ExclusionPredicateType = typeof(ExcludeNestedTypesPredicate) )]
    internal class BaseClass { }

    internal class CorrectNameFactory : BaseClass
    {
        private class IgnoredBecauseNestedClass : BaseClass { }
    }

    internal class IncorrectName : BaseClass { }

    internal class ExcludeNestedTypesPredicate : ReferencePredicate
    {
        public ExcludeNestedTypesPredicate( ReferencePredicateBuilder builder ) : base( builder ) { }

        protected override bool IsMatchCore( ReferenceValidationContext context ) => context.Origin.Type.DeclaringType != null;

        protected override ReferenceGranularity GetGranularity() => ReferenceGranularity.Type;
    }
}