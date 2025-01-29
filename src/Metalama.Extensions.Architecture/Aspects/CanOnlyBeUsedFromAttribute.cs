// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Extensions.Architecture.Predicates;
using Metalama.Extensions.Architecture.Validators;
using Metalama.Extensions.Validation;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;

namespace Metalama.Extensions.Architecture.Aspects;

/// <summary>
/// Aspect that, when applied to a type or member, reports a warning whenever the target type or member is used from a different type
/// than the ones specified by the <see cref="BaseUsageValidationAttribute.Types"/>, <see cref="BaseUsageValidationAttribute.Namespaces"/>,
/// <see cref="BaseUsageValidationAttribute.NamespaceOfTypes"/> or <see cref="BaseUsageValidationAttribute.CurrentNamespace"/> properties.
/// </summary>
[PublicAPI]
public class CanOnlyBeUsedFromAttribute : BaseUsageValidationAttribute, IAspect<IMemberOrNamedType>
{
    public void BuildEligibility( IEligibilityBuilder<IMemberOrNamedType> builder ) { }

    public void BuildAspect( IAspectBuilder<IMemberOrNamedType> builder )
    {
        var predicateBuilder = new ReferencePredicateBuilder( ReferenceEndRole.Origin, builder );

        if ( !this.TryCreatePredicate( builder, predicateBuilder, out var predicate )
             || !this.TryCreateExclusionPredicate( builder, predicateBuilder, out var exclusionPredicate ) )
        {
            return;
        }

        builder.Outbound.ValidateInboundReferences(
            new ReferencePredicateValidator(
                new HasFamilyAccessPredicate( new ReferencePredicateBuilder( ReferenceEndRole.Origin, builder ) )
                    .Or( predicate )
                    .Or( exclusionPredicate ),
                this.Description,
                this.ReferenceKinds ) );
    }
}