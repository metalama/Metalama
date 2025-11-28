// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using System;

namespace Metalama.Framework.Aspects
{
    /// <summary>
    /// A base class for aspects that target event declarations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class provides a convenient base for creating event-level aspects by implementing <see cref="IAspect{T}"/>
    /// with <typeparamref name="T"/> set to <see cref="IEvent"/>. Derived classes override <see cref="BuildAspect"/>
    /// to add advice (such as overriding event accessors or invoke semantics, adding attributes, or introducing related members)
    /// to the target event.
    /// </para>
    /// <para>
    /// For aspects that specifically override event semantics (add, remove, or invoke operations) with templates, consider deriving from
    /// <see cref="OverrideEventAspect"/> instead, which provides a simpler template-based API for overriding event add, remove, and
    /// invoke operations.
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
    /// <seealso cref="IEvent"/>
    /// <seealso cref="OverrideEventAspect"/>
    /// <seealso cref="IAspectBuilder{T}"/>
    /// <seealso cref="Aspect"/>
    /// <seealso href="@aspects"/>
    /// <seealso href="@overriding-events"/>
    [AttributeUsage( AttributeTargets.Event )]
    public abstract class EventAspect : Aspect, IAspect<IEvent>
    {
        /// <inheritdoc />
        public virtual void BuildAspect( IAspectBuilder<IEvent> builder ) { }

        /// <inheritdoc />
        public virtual void BuildEligibility( IEligibilityBuilder<IEvent> builder )
        {
            builder.DeclaringType().MustBeRunTimeOnly();
        }
    }
}