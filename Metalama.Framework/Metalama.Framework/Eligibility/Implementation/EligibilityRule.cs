// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;

namespace Metalama.Framework.Eligibility.Implementation
{
    internal sealed class EligibilityRule<T> : IEligibilityRule<T>
        where T : class
    {
        private readonly EligibleScenarios _ineligibility;
        private readonly Predicate<T> _predicate;
        private readonly Func<IDescribedObject<T>, FormattableString> _getJustification;

        public EligibilityRule( EligibleScenarios ineligibility, Predicate<T> predicate, Func<IDescribedObject<T>, FormattableString> getJustification )
        {
            this._ineligibility = ineligibility;
            this._predicate = predicate;
            this._getJustification = getJustification;
        }

        public static IEligibilityRule<T> Empty { get; } = new EligibilityRule<T>(
            EligibleScenarios.All,
            _ => true,
            _ => throw new InvalidOperationException() );

        public EligibleScenarios GetEligibility( T obj ) => this._predicate( obj ) ? EligibleScenarios.All : this._ineligibility;

        public FormattableString GetIneligibilityJustification( EligibleScenarios requestedEligibility, IDescribedObject<T> describedObject )
            => this._getJustification( describedObject );
    }
}