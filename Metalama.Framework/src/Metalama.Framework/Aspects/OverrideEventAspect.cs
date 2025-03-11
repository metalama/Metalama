// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using System;

namespace Metalama.Framework.Aspects
{
    /// <summary>
    /// A base aspect that overrides the implementation of an event.
    /// </summary>
    /// <seealso href="@overriding-events"/>
    [AttributeUsage( AttributeTargets.Event )]
    public abstract class OverrideEventAspect : EventAspect
    {
        /// <inheritdoc />
        public override void BuildAspect( IAspectBuilder<IEvent> builder )
        {
            builder.OverrideAccessors(
                nameof(this.OverrideAdd),
                nameof(this.OverrideRemove) );
        }

        // TODO: When template parameters are properly resolved during expansion, the parameter name here should change to "handler".
        [Template]
        public abstract void OverrideAdd( dynamic value );

        [Template]
        public abstract void OverrideRemove( dynamic value );

        // TODO: Add this back after invoke overrides are implemented.
        // [Template]
        // public abstract void OverrideInvoke( dynamic handler );

        public override void BuildEligibility( IEligibilityBuilder<IEvent> builder )
        {
            builder.AddRule( EligibilityRuleFactory.OverrideEventAdviceRule );
        }
    }
}