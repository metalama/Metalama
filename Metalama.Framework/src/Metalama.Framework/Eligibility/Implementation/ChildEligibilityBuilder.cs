// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Utilities;
using System;

namespace Metalama.Framework.Eligibility.Implementation
{
    internal sealed class ChildEligibilityBuilder<TParent, TChild> : IEligibilityBuilder<TChild>
        where TChild : class
        where TParent : class
    {
        private readonly IEligibilityBuilder<TParent> _parent;
        private readonly ChildAccessor _accessor;

        public ChildEligibilityBuilder(
            IEligibilityBuilder<TParent> parent,
            [Durable] Func<TParent, TChild> getChild,
            [Durable] Func<IDescribedObject<TParent>, FormattableString> getChildDescription,
            [Durable] Predicate<TParent>? canGetChild = null,
            [Durable] Func<IDescribedObject<TParent>, FormattableString>? cannotGetChildJustification = null )
        {
            if ( canGetChild != null && cannotGetChildJustification == null )
            {
                throw new ArgumentNullException( nameof(cannotGetChildJustification), "This argument must be specified when 'canGetChild' is specified." );
            }

            this._parent = parent;
            this._accessor = new ChildAccessor( getChild, getChildDescription, canGetChild, cannotGetChildJustification );
        }

        public EligibleScenarios IneligibleScenarios => this._parent.IneligibleScenarios;

        /// <remarks>
        /// The rule is given the accessor and the ineligibility rather than this builder. A rule outlives the builder
        /// that produced it, so holding the builder would keep the whole chain of builders alive, up to the root and
        /// its list of every sibling rule, and would make a type that must be immutable hold one that must not be.
        /// Reading the ineligibility here rather than in the rule matches <see cref="EligibilityRule{T}"/>, which is
        /// passed the same value at the same moment. It is fixed from the moment a builder exists, because every
        /// implementation of the property is either a constant or assigned in a constructor.
        /// </remarks>
        public void AddRule( IEligibilityRule<TChild> rule )
            => this._parent.AddRule( new ChildRule( this._accessor, this.IneligibleScenarios, rule ) );

        // This method is not supported because the predicates are added to the parent. This class is never used alone.
        IEligibilityRule<IDeclaration> IEligibilityBuilder.Build()
            => throw new NotSupportedException( $"The {nameof(IEligibilityBuilder.Build)} method must be called on the parent builder." );

        /// <summary>
        /// The delegates that say how to reach a child from a parent and how to describe it.
        /// </summary>
        /// <remarks>
        /// They are held apart from the builder so that a rule can keep them without keeping the builder. One instance
        /// is built per builder and shared by every rule that builder produces, so adding many rules to one child
        /// builder costs one reference each rather than four.
        /// </remarks>
        [Durable]
        [ImmutableType]
        private sealed class ChildAccessor
        {
            public ChildAccessor(
                [Durable] Func<TParent, TChild> getChild,
                [Durable] Func<IDescribedObject<TParent>, FormattableString> getChildDescription,
                [Durable] Predicate<TParent>? canGetChild,
                [Durable] Func<IDescribedObject<TParent>, FormattableString>? cannotGetChildJustification )
            {
                this.GetChild = getChild;
                this.GetChildDescription = getChildDescription;
                this.CanGetChild = canGetChild;
                this.CannotGetChildJustification = cannotGetChildJustification;
            }

            [Durable]
            public Func<TParent, TChild> GetChild { get; }

            [Durable]
            public Func<IDescribedObject<TParent>, FormattableString> GetChildDescription { get; }

            [Durable]
            public Predicate<TParent>? CanGetChild { get; }

            /// <remarks>
            /// Never null when <see cref="CanGetChild"/> is not null, which the constructor of the builder checks.
            /// </remarks>
            [Durable]
            public Func<IDescribedObject<TParent>, FormattableString>? CannotGetChildJustification { get; }
        }

        private sealed class ChildRule : IEligibilityRule<TParent>
        {
            private readonly ChildAccessor _accessor;
            private readonly EligibleScenarios _ineligibility;
            private readonly IEligibilityRule<TChild> _childRule;

            public ChildRule( ChildAccessor accessor, EligibleScenarios ineligibility, IEligibilityRule<TChild> childRule )
            {
                this._accessor = accessor;
                this._ineligibility = ineligibility;
                this._childRule = childRule;
            }

            public EligibleScenarios GetEligibility( TParent obj )
            {
                if ( this._accessor.CanGetChild != null && !this._accessor.CanGetChild( obj ) )
                {
                    return this._ineligibility;
                }

                return this._childRule.GetEligibility( this._accessor.GetChild( obj ) );
            }

            public FormattableString? GetIneligibilityJustification(
                EligibleScenarios requestedEligibility,
                IDescribedObject<TParent> describedObject )
            {
                if ( this._accessor.CanGetChild != null && !this._accessor.CanGetChild( describedObject.Object ) )
                {
                    return this._accessor.CannotGetChildJustification!( describedObject );
                }

                var child = this._accessor.GetChild( describedObject.Object );

                return this._childRule.GetIneligibilityJustification(
                    requestedEligibility,
                    new DescribedObject<TChild>(
                        child,
                        this._accessor.GetChildDescription( describedObject ) ) );
            }
        }
    }
}
