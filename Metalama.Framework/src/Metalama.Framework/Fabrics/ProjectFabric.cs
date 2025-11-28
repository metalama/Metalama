// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Fabrics
{
    /// <summary>
    /// A compile-time entry point that executes within the compiler and IDE to add aspects, configure libraries, and implement architecture rules
    /// for the current project.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Project fabrics are unique classes that execute at compile time and design time. Unlike aspects, fabrics do not need to be applied
    /// to any declaration or called from anywhere—their primary method (<see cref="AmendProject"/>) is invoked automatically simply because
    /// the class exists in your code. Think of fabrics as compile-time entry points.
    /// </para>
    /// <para>
    /// With project fabrics, you can:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Add aspects programmatically using LINQ-like code queries instead of marking individual declarations with custom attributes.</description></item>
    /// <item><description>Configure aspect libraries for the current project.</description></item>
    /// <item><description>Implement and enforce architecture rules and validation.</description></item>
    /// </list>
    /// <para>
    /// A <see cref="ProjectFabric"/> applies transformations only to the project in which it is defined. To apply transformations to projects that
    /// reference the current project, use <see cref="TransitiveProjectFabric"/> instead.
    /// </para>
    /// <para>
    /// When a project contains multiple project fabrics, they are ordered by source file location (closest to the root directory first),
    /// then by type name.
    /// </para>
    /// </remarks>
    /// <seealso cref="Fabric"/>
    /// <seealso cref="IProjectAmender"/>
    /// <seealso cref="NamespaceFabric"/>
    /// <seealso cref="TypeFabric"/>
    /// <seealso cref="TransitiveProjectFabric"/>
    /// <seealso href="@fabrics"/>
    /// <seealso href="@aspect-configuration"/>
    /// <seealso href="@fabrics-adding-aspects"/>
    /// <seealso href="@validation"/>
    public abstract class ProjectFabric : Fabric
    {
        /// <summary>
        /// Implement this method to programmatically analyze the current project, add aspects, configure options, validate architecture, and report or suppress diagnostics.
        /// This method is invoked automatically at compile time and design time.
        /// </summary>
        /// <param name="amender">An object that provides access to query declarations in the project, add aspects using LINQ-like queries, configure options, and report diagnostics.</param>
        /// <seealso href="@fabrics-adding-aspects"/>
        /// <seealso href="@aspect-configuration"/>
        /// <seealso href="@validation"/>
        public abstract void AmendProject( IProjectAmender amender );
    }
}