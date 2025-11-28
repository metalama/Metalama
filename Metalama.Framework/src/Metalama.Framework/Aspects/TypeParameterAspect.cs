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
    /// A base class for aspects that target generic type parameter declarations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class provides a convenient base for creating generic type parameter aspects by implementing <see cref="IAspect{T}"/>
    /// with <typeparamref name="T"/> set to <see cref="ITypeParameter"/>. Derived classes override <see cref="BuildAspect"/>
    /// to add advice (such as adding constraints, attributes, or validation) to the target generic type parameter.
    /// </para>
    /// <para>
    /// This is a convenience base class. The aspect framework primarily requires implementation of <see cref="IAspect{T}"/>.
    /// </para>
    /// </remarks>
    /// <seealso cref="IAspect{T}"/>
    /// <seealso cref="ITypeParameter"/>
    /// <seealso cref="IAspectBuilder{T}"/>
    /// <seealso cref="Aspect"/>
    /// <seealso href="@aspects"/>
    [AttributeUsage( AttributeTargets.GenericParameter )]
    [PublicAPI]
    public abstract class TypeParameterAspect : Aspect, IAspect<ITypeParameter>
    {
        /// <inheritdoc />
        public virtual void BuildAspect( IAspectBuilder<ITypeParameter> builder ) { }

        /// <inheritdoc />
        public virtual void BuildEligibility( IEligibilityBuilder<ITypeParameter> builder ) { }
    }
}