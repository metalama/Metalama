// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Extensions.Validation;
using Metalama.Framework.Code;

namespace Metalama.Extensions.Architecture.Predicates;

internal class HasFamilyAccessPredicate : ReferencePredicate
{
    private readonly ReferenceEndRole _role;

    public HasFamilyAccessPredicate( ReferencePredicateBuilder builder ) : base( builder )
    {
        this._role = builder.Context.ValidatedRole;
    }

    protected override bool IsMatchCore( ReferenceValidationContext context )
    {
        // TODO: take nested types into account.

        var b = context.GetReferenceEnd( this._role );
        var a = context.GetReferenceEnd( this._role == ReferenceEndRole.Destination ? ReferenceEndRole.Origin : ReferenceEndRole.Destination );

        return a.Declaration is IMemberOrNamedType { Accessibility: Accessibility.Protected or Accessibility.ProtectedInternal }
               && b.Type.IsConvertibleTo( a.Member.GetClosestNamedType()! );
    }

    protected override ReferenceGranularity GetGranularity() => ReferenceGranularity.Type;
}