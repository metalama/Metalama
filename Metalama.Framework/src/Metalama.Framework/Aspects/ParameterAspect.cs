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
    /// A base class for aspects that target method, constructor, or indexer parameter declarations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class provides a convenient base for creating parameter-level aspects by implementing <see cref="IAspect{T}"/>
    /// with <c>T</c> set to <see cref="IParameter"/>. Derived classes override <see cref="BuildAspect"/>
    /// to add advice (such as adding contracts for parameter validation, introducing attributes, or applying constraints)
    /// to the target parameter.
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
    /// <seealso cref="IParameter"/>
    /// <seealso cref="IAspectBuilder{T}"/>
    /// <seealso cref="Aspect"/>
    /// <seealso cref="ContractAspect"/>
    /// <seealso href="@aspects"/>
    /// <seealso href="@contracts"/>
    [AttributeUsage( AttributeTargets.Parameter )]
    [PublicAPI]
    public abstract class ParameterAspect : Aspect, IAspect<IParameter>
    {
        /// <inheritdoc />
        public virtual void BuildAspect( IAspectBuilder<IParameter> builder ) { }

        /// <inheritdoc />
        public virtual void BuildEligibility( IEligibilityBuilder<IParameter> builder )
        {
            builder.DeclaringMember().DeclaringType().MustBeRunTimeOnly();
        }
    }
}