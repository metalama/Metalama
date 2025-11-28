// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using System;

namespace Metalama.Framework.Aspects
{
    /// <summary>
    /// A base class for aspects that target method declarations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class provides a convenient base for creating method-level aspects by implementing <see cref="IAspect{T}"/>
    /// with <c>T</c> set to <see cref="IMethod"/>. Derived classes override <see cref="BuildAspect"/>
    /// to add advice (such as overriding the method implementation, adding contracts, or introducing attributes) to the target method.
    /// </para>
    /// <para>
    /// For aspects that specifically override method implementations, consider deriving from <see cref="OverrideMethodAspect"/>
    /// instead, which provides a simpler template-based API.
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
    /// <seealso cref="OverrideMethodAspect"/>
    /// <seealso cref="IAspectBuilder{T}"/>
    /// <seealso cref="IMethod"/>
    /// <seealso cref="Aspect"/>
    /// <seealso href="@aspects"/>
    [AttributeUsage( AttributeTargets.Method )]
    public abstract class MethodAspect : Aspect, IAspect<IMethod>
    {
        /// <inheritdoc />
        public virtual void BuildAspect( IAspectBuilder<IMethod> builder ) { }

        /// <inheritdoc />
        public virtual void BuildEligibility( IEligibilityBuilder<IMethod> builder )
        {
            builder.DeclaringType().MustBeRunTimeOnly();
        }
    }
}