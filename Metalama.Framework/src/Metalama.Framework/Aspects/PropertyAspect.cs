// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using System;

namespace Metalama.Framework.Aspects
{
    /// <summary>
    /// A base class for aspects that target property declarations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class provides a convenient base for creating property-level aspects by implementing <see cref="IAspect{T}"/>
    /// with <c>T</c> set to <see cref="IProperty"/>. Derived classes override <see cref="BuildAspect"/>
    /// to add advice (such as overriding property accessors, adding contracts, or introducing attributes) to the target property.
    /// </para>
    /// <para>
    /// For aspects that need to handle both fields and properties uniformly, consider deriving from <see cref="FieldOrPropertyAspect"/>
    /// instead. For aspects that specifically override property accessor implementations with templates, consider deriving from
    /// <see cref="OverrideFieldOrPropertyAspect"/>, which provides a simpler template-based API.
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
    /// <seealso cref="IAspectBuilder{T}"/>
    /// <seealso cref="IProperty"/>
    /// <seealso cref="FieldOrPropertyAspect"/>
    /// <seealso cref="OverrideFieldOrPropertyAspect"/>
    /// <seealso cref="Aspect"/>
    /// <seealso href="@aspects"/>
    /// <seealso href="@overriding-fields-or-properties"/>
    [AttributeUsage( AttributeTargets.Property )]
    [PublicAPI]
    public abstract class PropertyAspect : Aspect, IAspect<IProperty>
    {
        /// <inheritdoc />
        public virtual void BuildAspect( IAspectBuilder<IProperty> builder ) { }

        /// <inheritdoc />
        public virtual void BuildEligibility( IEligibilityBuilder<IProperty> builder )
        {
            builder.DeclaringType().MustBeRunTimeOnly();
        }
    }
}