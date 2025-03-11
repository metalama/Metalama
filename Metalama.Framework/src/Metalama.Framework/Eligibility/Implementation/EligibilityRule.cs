// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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