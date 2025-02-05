// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Eligibility.Implementation
{
    internal sealed class ChildEligibilityBuilder<TParent, TChild> : IEligibilityBuilder<TChild>
        where TChild : class
        where TParent : class
    {
        private readonly IEligibilityBuilder<TParent> _parent;
        private readonly Func<TParent, TChild> _getChild;
        private readonly Func<IDescribedObject<TParent>, FormattableString> _getChildDescription;
        private readonly Predicate<TParent>? _canGetChild;
        private readonly Func<IDescribedObject<TParent>, FormattableString>? _cannotGetChildJustification;

        public ChildEligibilityBuilder(
            IEligibilityBuilder<TParent> parent,
            Func<TParent, TChild> getChild,
            Func<IDescribedObject<TParent>, FormattableString> getChildDescription,
            Predicate<TParent>? canGetChild = null,
            Func<IDescribedObject<TParent>, FormattableString>? cannotGetChildJustification = null )
        {
            this._parent = parent;
            this._canGetChild = canGetChild;
            this._cannotGetChildJustification = cannotGetChildJustification;
            this._getChild = getChild;
            this._getChildDescription = getChildDescription;

            if ( canGetChild != null && cannotGetChildJustification == null )
            {
                throw new ArgumentNullException( nameof(cannotGetChildJustification), "This argument must be specified when 'canGetChild' is specified." );
            }
        }

        public EligibleScenarios IneligibleScenarios => this._parent.IneligibleScenarios;

        public void AddRule( IEligibilityRule<TChild> rule ) => this._parent.AddRule( new ChildRule( this, rule ) );

        // This method is not supported because the predicates are added to the parent. This class is never used alone. 
        IEligibilityRule<IDeclaration> IEligibilityBuilder.Build()
            => throw new NotSupportedException( $"The {nameof(IEligibilityBuilder.Build)} method must be called on the parent builder." );

        private sealed class ChildRule : IEligibilityRule<TParent>
        {
            private readonly ChildEligibilityBuilder<TParent, TChild> _parent;
            private readonly IEligibilityRule<TChild> _childRule;

            public ChildRule( ChildEligibilityBuilder<TParent, TChild> parent, IEligibilityRule<TChild> childRule )
            {
                this._parent = parent;
                this._childRule = childRule;
            }

            public EligibleScenarios GetEligibility( TParent obj )
            {
                if ( this._parent._canGetChild != null && !this._parent._canGetChild( obj ) )
                {
                    return this._parent.IneligibleScenarios;
                }

                return this._childRule.GetEligibility( this._parent._getChild( obj ) );
            }

            public FormattableString? GetIneligibilityJustification(
                EligibleScenarios requestedEligibility,
                IDescribedObject<TParent> describedObject )
            {
                if ( this._parent._canGetChild != null && !this._parent._canGetChild( describedObject.Object ) )
                {
                    return this._parent._cannotGetChildJustification!( describedObject );
                }

                var child = this._parent._getChild( describedObject.Object );

                return this._childRule.GetIneligibilityJustification(
                    requestedEligibility,
                    new DescribedObject<TChild>(
                        child,
                        this._parent._getChildDescription( describedObject ) ) );
            }
        }
    }
}