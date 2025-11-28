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
    /// A base class for aspects that target constructor declarations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class provides a convenient base for creating constructor-level aspects by implementing <see cref="IAspect{T}"/>
    /// with <c>T</c> set to <see cref="IConstructor"/>. Derived classes override <see cref="BuildAspect"/>
    /// to add advice (such as overriding constructor implementations, adding initializers, introducing parameters, or adding contracts)
    /// to the target constructor.
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
    /// <seealso cref="IConstructor"/>
    /// <seealso cref="IAspectBuilder{T}"/>
    /// <seealso cref="Aspect"/>
    /// <seealso href="@aspects"/>
    /// <seealso href="@overriding-constructors"/>
    /// <seealso href="@initializers"/>
    /// <seealso href="@introducing-constructor-parameters"/>
    [AttributeUsage( AttributeTargets.Constructor )]
    [PublicAPI]
    public abstract class ConstructorAspect : Aspect, IAspect<IConstructor>
    {
        /// <inheritdoc />
        public virtual void BuildAspect( IAspectBuilder<IConstructor> builder ) { }

        /// <inheritdoc />
        public virtual void BuildEligibility( IEligibilityBuilder<IConstructor> builder )
        {
            builder.DeclaringType().MustBeRunTimeOnly();
        }
    }
}