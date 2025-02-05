// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Metalama.Framework.Eligibility.Implementation
{
    internal sealed class EligibilityBuilder<T> : IEligibilityBuilder<T>
        where T : class
    {
        private readonly List<IEligibilityRule<T>> _predicates = new();
        private readonly BooleanCombinationOperator _combinationOperator;

        public EligibilityBuilder( BooleanCombinationOperator combinationOperator = BooleanCombinationOperator.And )
        {
            this._combinationOperator = combinationOperator;
        }

        public EligibleScenarios IneligibleScenarios => EligibleScenarios.None;

        public void AddRule( IEligibilityRule<T> rule ) => this._predicates.Add( rule );

        IEligibilityRule<IDeclaration> IEligibilityBuilder.Build() => new CastEligibilityRule<T, object>( this.Build() );

        public IEligibilityRule<T> Build()
        {
            switch ( this._predicates.Count )
            {
                case 0:
                    return EligibilityRule<T>.Empty;

                case 1:
                    return this._predicates[0];

                default:
                    {
                        var predicates = this._predicates.ToImmutableArray();

                        return this._combinationOperator == BooleanCombinationOperator.Or
                            ? new OrEligibilityRule<T>( predicates )
                            : new AndEligibilityRule<T>( predicates );
                    }
            }
        }
    }
}