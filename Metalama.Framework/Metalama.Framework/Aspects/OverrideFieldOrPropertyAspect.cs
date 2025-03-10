// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Aspects
{
    /// <summary>
    /// A base aspect that overrides the implementation of a field or a property.
    /// </summary>
    /// <seealso href="@overriding-fields-or-properties"/>
    [AttributeUsage( AttributeTargets.Property | AttributeTargets.Field )]
    public abstract class OverrideFieldOrPropertyAspect : FieldOrPropertyAspect
    {
        /// <inheritdoc />
        public override void BuildAspect( IAspectBuilder<IFieldOrProperty> builder )
        {
            var getterTemplateSelector = new GetterTemplateSelector(
                "get_" + nameof(this.OverrideProperty),
                "get_" + nameof(this.OverrideEnumerableProperty),
                "get_" + nameof(this.OverrideEnumeratorProperty) );

            builder.OverrideAccessors( getterTemplateSelector, "set_" + nameof(this.OverrideProperty) );
        }

        /// <inheritdoc />
        public override void BuildEligibility( IEligibilityBuilder<IFieldOrProperty> builder )
        {
            builder.AddRule( EligibilityRuleFactory.OverrideFieldOrPropertyOrIndexerAdviceRule );
        }

        [Template]
        public abstract dynamic? OverrideProperty
        {
            get;
            set;
        }

        [Template( IsEmpty = true )]
        public virtual IEnumerable<dynamic?> OverrideEnumerableProperty => throw new NotSupportedException();

        [Template( IsEmpty = true )]
        public virtual IEnumerator<dynamic?> OverrideEnumeratorProperty => throw new NotSupportedException();
    }
}