// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Extensions.Architecture.Predicates;
using Metalama.Extensions.Architecture.Validators;
using Metalama.Extensions.Validation;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using System.Linq;

namespace Metalama.Extensions.Architecture.Aspects;

/// <summary>
/// A base class for <see cref="InternalsCannotBeUsedFromAttribute"/> and <see cref="InternalsCanOnlyBeUsedFromAttribute"/>.
/// </summary>
public abstract class InternalsUsageValidationAttribute : BaseUsageValidationAttribute, IAspect<INamedType>
{
    public virtual void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var predicateBuilder = new ReferencePredicateBuilder( ReferenceEndRole.Origin, builder );

        if ( !this.TryCreatePredicate( builder, predicateBuilder, out var predicate )
             || !this.TryCreateExclusionPredicate( builder, predicateBuilder, out var exclusionPredicate ) )
        {
            return;
        }

        var validator = this.CreateValidator( predicate, exclusionPredicate );

        // Register a validator for all internal members.
        builder.Outbound.SelectMany(
                t => t.Members().Where( m => m.Accessibility is Accessibility.Internal or Accessibility.PrivateProtected or Accessibility.ProtectedInternal ) )
            .ValidateInboundReferences( validator );

        // Also register internal accessors of public properties.
        builder.Outbound.SelectMany( t => t.Properties.Where( p => p.Accessibility is Accessibility.Public or Accessibility.Protected ) )
            .SelectMany(
                p => p.Accessors.Where( m => m.Accessibility is Accessibility.Internal or Accessibility.PrivateProtected or Accessibility.ProtectedInternal ) )
            .ValidateInboundReferences( validator );
    }

    protected abstract ReferencePredicateValidator CreateValidator(
        ReferencePredicate predicate,
        ReferencePredicate? exclusionPredicate );

    public virtual void BuildEligibility( IEligibilityBuilder<INamedType> builder ) => builder.MustHaveAccessibility( Accessibility.Public );
}