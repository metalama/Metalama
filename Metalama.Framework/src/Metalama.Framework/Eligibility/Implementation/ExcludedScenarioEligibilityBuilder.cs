// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;

namespace Metalama.Framework.Eligibility.Implementation
{
    internal sealed class ExcludedScenarioEligibilityBuilder<T> : IEligibilityBuilder<T>
        where T : class
    {
        private readonly IEligibilityBuilder<T> _inner;

        public ExcludedScenarioEligibilityBuilder( IEligibilityBuilder<T> inner, EligibleScenarios excludedScenario )
        {
            this._inner = inner;
            this.IneligibleScenarios = excludedScenario;
        }

        public EligibleScenarios IneligibleScenarios { get; }

        public void AddRule( IEligibilityRule<T> rule ) => this._inner.AddRule( rule );

        public IEligibilityRule<IDeclaration> Build() => this._inner.Build();
    }
}