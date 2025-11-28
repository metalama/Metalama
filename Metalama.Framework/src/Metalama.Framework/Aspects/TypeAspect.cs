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
    /// A base class for aspects that target type declarations (classes, structs, interfaces, delegates, and enums).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class provides a convenient base for creating type-level aspects by implementing <see cref="IAspect{T}"/>
    /// with <c>T</c> set to <see cref="INamedType"/>. Derived classes override <see cref="BuildAspect"/>
    /// to add advice (such as introducing members, implementing interfaces, or applying child aspects) to the target type.
    /// </para>
    /// <para>
    /// Aspects can only be applied to run-time code, never to compile-time types (types marked with <see cref="CompileTimeAttribute"/>
    /// or <see cref="RunTimeOrCompileTimeAttribute"/>). This eligibility restriction is enforced by the <see cref="BuildEligibility"/>
    /// method.
    /// </para>
    /// <para>
    /// This is a convenience base class. The aspect framework primarily requires implementation of <see cref="IAspect{T}"/>.
    /// </para>
    /// </remarks>
    /// <seealso cref="IAspect{T}"/>
    /// <seealso cref="IAspectBuilder{T}"/>
    /// <seealso cref="INamedType"/>
    /// <seealso cref="Aspect"/>
    /// <seealso href="@aspects"/>
    [AttributeUsage( AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Delegate | AttributeTargets.Enum )]
    [PublicAPI]
    public abstract class TypeAspect : Aspect, IAspect<INamedType>
    {
        /// <inheritdoc />
        public virtual void BuildAspect( IAspectBuilder<INamedType> builder ) { }

        /// <inheritdoc />
        public virtual void BuildEligibility( IEligibilityBuilder<INamedType> builder )
        {
            builder.MustBeRunTimeOnly();
        }
    }
}