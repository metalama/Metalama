// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Extensions.Validation;

namespace Metalama.Extensions.Architecture.Predicates;

internal sealed class NotPredicateModifier : PredicateModifier
{
    private readonly PredicateModifier? _previousModifier;

    public NotPredicateModifier( ReferencePredicateBuilder builder )
    {
        this._previousModifier = builder.Modifier;
    }

    public override bool IsMatch( bool currentPredicateResult, ReferenceValidationContext context )
    {
        var result = !currentPredicateResult;

        if ( this._previousModifier != null )
        {
            return this._previousModifier.IsMatch( result, context );
        }
        else
        {
            return result;
        }
    }

    public override ReferenceGranularity ModifyGranularity( ReferenceGranularity baseGranularity ) => baseGranularity;
}