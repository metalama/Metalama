// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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