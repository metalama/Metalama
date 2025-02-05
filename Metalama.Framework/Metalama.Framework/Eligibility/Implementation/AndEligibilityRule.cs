// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.Collections.Immutable;

namespace Metalama.Framework.Eligibility.Implementation
{
    internal sealed class AndEligibilityRule<T> : IEligibilityRule<T>
        where T : class
    {
        private readonly ImmutableArray<IEligibilityRule<T>> _rules;

        public AndEligibilityRule( ImmutableArray<IEligibilityRule<T>> rules )
        {
            this._rules = rules;
        }

        public EligibleScenarios GetEligibility( T obj )
        {
            var eligibility = EligibleScenarios.All;

            foreach ( var predicate in this._rules )
            {
                eligibility &= predicate.GetEligibility( obj );

                if ( eligibility == EligibleScenarios.None )
                {
                    return EligibleScenarios.None;
                }
            }

            return eligibility;
        }

        public FormattableString? GetIneligibilityJustification(
            EligibleScenarios requestedEligibility,
            IDescribedObject<T> describedObject )
        {
            foreach ( var predicate in this._rules )
            {
                var eligibility = predicate.GetEligibility( describedObject.Object );

                if ( (eligibility & requestedEligibility) != requestedEligibility )
                {
                    return predicate.GetIneligibilityJustification( requestedEligibility, describedObject );
                }
            }

            return null;
        }
    }
}