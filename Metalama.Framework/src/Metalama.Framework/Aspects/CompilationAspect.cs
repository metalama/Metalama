// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Fabrics;
using System;

namespace Metalama.Framework.Aspects
{
    /// <summary>
    /// A base class for aspects that target the compilation level, applied using assembly-level custom attributes (e.g., <c>[assembly: MyAspect]</c>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class provides a convenient base for creating compilation-level aspects by implementing <see cref="IAspect{T}"/>
    /// with <c>T</c> set to <see cref="ICompilation"/>. Derived classes override <see cref="BuildAspect"/>
    /// to perform project-wide transformations, such as applying aspects to types meeting certain criteria, introducing
    /// project-level configuration, or validating project-wide conventions.
    /// </para>
    /// <para>
    /// <strong>Note:</strong> In most scenarios, providing an <see cref="IAmender{T}"/> extension method that can be called
    /// from a fabric is preferred over using compilation aspects. Extension methods provide a programmatic API for project-wide
    /// transformations and offer better discoverability and composability, whereas compilation aspects require explicit
    /// declaration using <c>[assembly: MyAspect]</c> syntax.
    /// </para>
    /// <para>
    /// For more localized transformations, consider using <see cref="TypeAspect"/> for type-level aspects or
    /// member-specific aspect base classes.
    /// </para>
    /// <para>
    /// This is a convenience base class. The aspect framework primarily requires implementation of <see cref="IAspect{T}"/>.
    /// </para>
    /// </remarks>
    /// <seealso cref="IAspect{T}"/>
    /// <seealso cref="IAspectBuilder{T}"/>
    /// <seealso cref="ICompilation"/>
    /// <seealso cref="IAmender{T}"/>
    /// <seealso cref="TypeAspect"/>
    /// <seealso cref="ProjectFabric"/>
    /// <seealso cref="TransitiveProjectFabric"/>
    /// <seealso cref="Aspect"/>
    /// <seealso href="@aspects"/>
    /// <seealso href="@fabrics"/>
    [AttributeUsage( AttributeTargets.Assembly )]
    [PublicAPI]
    public abstract class CompilationAspect : Aspect, IAspect<ICompilation>
    {
        /// <inheritdoc />
        public virtual void BuildAspect( IAspectBuilder<ICompilation> builder ) { }

        /// <inheritdoc />
        public virtual void BuildEligibility( IEligibilityBuilder<ICompilation> builder ) { }
    }
}