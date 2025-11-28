// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;

namespace Metalama.Framework.Aspects
{
    /// <summary>
    /// Custom attribute that, when applied to an aspect class, causes all instances of this aspect
    /// to be inherited by derived declarations. The aspect's <see cref="IAspect{T}.BuildAspect"/> method is invoked
    /// not only for the direct target declaration but also for all derived declarations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Aspects marked with this attribute are inherited in the following scenarios:
    /// </para>
    /// <list type="bullet">
    /// <item><description>From a base class to all derived classes</description></item>
    /// <item><description>From a base interface to all derived interfaces</description></item>
    /// <item><description>From an interface to all types implementing that interface</description></item>
    /// <item><description>From a <c>virtual</c> or <c>abstract</c> member to its <c>override</c> members</description></item>
    /// <item><description>From an interface member to its implementations</description></item>
    /// <item><description>From a parameter of a <c>virtual</c> or <c>abstract</c> method to the corresponding parameter of all <c>override</c> methods</description></item>
    /// <item><description>From a parameter of an interface member to the corresponding parameter of all its implementations</description></item>
    /// </list>
    /// <para>
    /// <b>Cross-project inheritance:</b> Aspect inheritance works across project boundaries. The aspect instance
    /// is serialized in the assembly containing the base declaration and deserialized when compiling projects
    /// that reference it.
    /// </para>
    /// <para>
    /// <b>Conditional inheritance:</b> For aspects that need to decide inheritability based on properties or context,
    /// implement <see cref="IConditionallyInheritableAspect"/> instead of using this attribute.
    /// </para>
    /// </remarks>
    /// <seealso cref="IConditionallyInheritableAspect"/>
    /// <seealso href="@aspect-inheritance"/>
    [AttributeUsage( AttributeTargets.Class )]
    [CompileTime]
    [PublicAPI]
    public sealed class InheritableAttribute : Attribute;
}