// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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