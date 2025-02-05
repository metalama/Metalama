// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Eligibility.Implementation
{
    internal sealed class ConditionalEligibilityBuilder<T> : IEligibilityBuilder<T>
        where T : class
    {
        private readonly Predicate<T> _condition;
        private readonly IEligibilityBuilder<T> _inner;

        public ConditionalEligibilityBuilder( IEligibilityBuilder<T> inner, Predicate<T> condition )
        {
            this._condition = condition;
            this._inner = inner;
        }

        public EligibleScenarios IneligibleScenarios => this._inner.IneligibleScenarios;

        public void AddRule( IEligibilityRule<T> rule ) => this._inner.AddRule( new ConditionalRule( this, rule ) );

        // This method is not supported because the predicates are added to the parent. This class is never used alone. 
        IEligibilityRule<IDeclaration> IEligibilityBuilder.Build() => throw new NotSupportedException();

        private sealed class ConditionalRule : IEligibilityRule<T>
        {
            private readonly ConditionalEligibilityBuilder<T> _parent;
            private readonly IEligibilityRule<T> _conditionalRule;

            public ConditionalRule( ConditionalEligibilityBuilder<T> parent, IEligibilityRule<T> conditionalRule )
            {
                this._parent = parent;
                this._conditionalRule = conditionalRule;
            }

            public EligibleScenarios GetEligibility( T obj )
            {
                if ( this._parent._condition( obj ) )
                {
                    return this._conditionalRule.GetEligibility( obj );
                }
                else
                {
                    return EligibleScenarios.All;
                }
            }

            public FormattableString? GetIneligibilityJustification( EligibleScenarios requestedEligibility, IDescribedObject<T> describedObject )
            {
                if ( this._parent._condition( describedObject.Object ) )
                {
                    var eligibility = this._conditionalRule.GetEligibility( describedObject.Object );

                    if ( (eligibility & requestedEligibility) != requestedEligibility )
                    {
                        return this._conditionalRule.GetIneligibilityJustification( requestedEligibility, describedObject );
                    }

                    return null;
                }
                else
                {
                    return null;
                }
            }
        }
    }
}