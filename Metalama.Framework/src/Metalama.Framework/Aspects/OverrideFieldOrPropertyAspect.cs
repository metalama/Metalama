// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Aspects
{
    /// <summary>
    /// A base aspect that overrides the implementation of a field or property using T# template properties.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class simplifies creating aspects that override field or property behavior. Derived classes must implement the
    /// <see cref="OverrideProperty"/> template property, which is a T# template that executes at compile-time to generate
    /// the new accessor implementations. Use <c>meta.Target.FieldOrProperty.Value</c> within the template to access or modify
    /// the underlying field or property value, and <c>meta.Target.FieldOrProperty</c> to access metadata.
    /// </para>
    /// <para>
    /// When applied to a field or auto-property, a backing field is automatically created to store the value,
    /// similar to how auto-properties are transformed.
    /// </para>
    /// <para>
    /// The aspect automatically selects the appropriate template based on the property's return type:
    /// <list type="bullet">
    /// <item><see cref="OverrideEnumerableProperty"/> for properties returning <see cref="IEnumerable{T}"/></item>
    /// <item><see cref="OverrideEnumeratorProperty"/> for properties returning <see cref="IEnumerator{T}"/></item>
    /// <item><see cref="OverrideProperty"/> for all other properties and fields</item>
    /// </list>
    /// If specialized templates are not overridden, <see cref="OverrideProperty"/> is used as the fallback.
    /// </para>
    /// <para>
    /// For overriding multiple fields or properties from a single aspect, or for separate control over getter and setter templates,
    /// use <see cref="AdviserExtensions.Override(IAdviser{IFieldOrProperty}, string, object?, object?)"/> or
    /// <see cref="AdviserExtensions.OverrideAccessors"/> from your <c>BuildAspect</c> method instead of deriving from this class.
    /// </para>
    /// </remarks>
    /// <seealso cref="FieldOrPropertyAspect"/>
    /// <seealso cref="GetterTemplateSelector"/>
    /// <seealso cref="AdviserExtensions.OverrideAccessors"/>
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

        /// <summary>
        /// The default template property for overriding field or property accessors.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is the primary template property that must be implemented in derived aspects. It is used for all fields and properties
        /// where a more specific template (<see cref="OverrideEnumerableProperty"/> or <see cref="OverrideEnumeratorProperty"/>)
        /// does not apply or has not been overridden.
        /// </para>
        /// <para>
        /// Within the template, use <c>meta.Target.FieldOrProperty.Value</c> to access the underlying field or property value,
        /// and <c>meta.Target.FieldOrProperty</c> to access metadata about the target.
        /// </para>
        /// <para>
        /// Due to C# language rules, the template property must implement both getter and setter if both are needed.
        /// For fine-grained control over individual accessors, derive from <see cref="FieldOrPropertyAspect"/> and use
        /// <see cref="AdviserExtensions.Override(IAdviser{IFieldOrProperty}, string, object?, object?)"/> to override specific accessors.
        /// </para>
        /// </remarks>
        [Template]
        public abstract dynamic? OverrideProperty
        {
            get;
            set;
        }

        /// <summary>
        /// Template property for overriding properties returning <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <remarks>
        /// This template is selected for properties returning <see cref="IEnumerable{T}"/>.
        /// If not overridden in a derived class, <see cref="OverrideProperty"/> is used instead.
        /// This template is marked with <c>[Template(IsEmpty = true)]</c>, making it optional to override.
        /// </remarks>
        [Template( IsEmpty = true )]
        public virtual IEnumerable<dynamic?> OverrideEnumerableProperty => throw new NotSupportedException();

        /// <summary>
        /// Template property for overriding properties returning <see cref="IEnumerator{T}"/>.
        /// </summary>
        /// <remarks>
        /// This template is selected for properties returning <see cref="IEnumerator{T}"/>.
        /// If not overridden in a derived class, <see cref="OverrideProperty"/> is used instead.
        /// This template is marked with <c>[Template(IsEmpty = true)]</c>, making it optional to override.
        /// </remarks>
        [Template( IsEmpty = true )]
        public virtual IEnumerator<dynamic?> OverrideEnumeratorProperty => throw new NotSupportedException();
    }
}