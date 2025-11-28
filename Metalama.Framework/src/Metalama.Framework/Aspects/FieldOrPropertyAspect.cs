// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using System;

namespace Metalama.Framework.Aspects
{
    /// <summary>
    /// A base class for aspects that target both field and property declarations uniformly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class provides a convenient base for creating aspects that apply to both fields and properties by implementing
    /// <see cref="IAspect{T}"/> with <typeparamref name="T"/> set to <see cref="IFieldOrProperty"/>. Derived classes override
    /// <see cref="BuildAspect"/> to add advice (such as overriding accessors, adding contracts, or introducing attributes)
    /// to the target field or property.
    /// </para>
    /// <para>
    /// For aspects that specifically override field or property accessors with templates, consider deriving from
    /// <see cref="OverrideFieldOrPropertyAspect"/> instead, which provides a simpler template-based API. For aspects
    /// that need to distinguish between fields and properties, use <see cref="FieldAspect"/> and <see cref="PropertyAspect"/>
    /// separately.
    /// </para>
    /// <para>
    /// Aspects can only be applied to run-time code, never to compile-time types or their members. This eligibility
    /// restriction is enforced by the <see cref="BuildEligibility"/> method.
    /// </para>
    /// <para>
    /// This is a convenience base class. The aspect framework primarily requires implementation of <see cref="IAspect{T}"/>.
    /// </para>
    /// </remarks>
    /// <seealso cref="IAspect{T}"/>
    /// <seealso cref="OverrideFieldOrPropertyAspect"/>
    /// <seealso cref="IAspectBuilder{T}"/>
    /// <seealso cref="IFieldOrProperty"/>
    /// <seealso cref="FieldAspect"/>
    /// <seealso cref="PropertyAspect"/>
    /// <seealso cref="Aspect"/>
    /// <seealso href="@aspects"/>
    /// <seealso href="@overriding-fields-or-properties"/>
    [AttributeUsage( AttributeTargets.Field | AttributeTargets.Property )]
    public abstract class FieldOrPropertyAspect : Aspect, IAspect<IFieldOrProperty>
    {
        /// <inheritdoc />
        public virtual void BuildAspect( IAspectBuilder<IFieldOrProperty> builder ) { }

        /// <inheritdoc />
        public virtual void BuildEligibility( IEligibilityBuilder<IFieldOrProperty> builder )
        {
            builder.DeclaringType().MustBeRunTimeOnly();
        }
    }
}