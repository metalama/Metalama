// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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