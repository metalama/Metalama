// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;

namespace Metalama.Framework.Eligibility.Implementation
{
    internal sealed class CastEligibilityRule<TIn, TOut> : IEligibilityRule<TOut>
        where TIn : class
        where TOut : class
    {
        private readonly IEligibilityRule<TIn> _inner;

        public CastEligibilityRule( IEligibilityRule<TIn> inner )
        {
            this._inner = inner;
        }

        public EligibleScenarios GetEligibility( TOut obj )
        {
            if ( obj is TIn convertedMember )
            {
                return this._inner.GetEligibility( convertedMember );
            }
            else
            {
                return EligibleScenarios.None;
            }
        }

        public FormattableString? GetIneligibilityJustification(
            EligibleScenarios requestedEligibility,
            IDescribedObject<TOut> describedObject )
        {
            if ( describedObject.Object is TIn )
            {
                return this._inner.GetIneligibilityJustification( requestedEligibility, describedObject.Cast<TOut, TIn>() );
            }
            else
            {
                return $"{describedObject} is not an {typeof(TIn).Name}";
            }
        }
    }
}