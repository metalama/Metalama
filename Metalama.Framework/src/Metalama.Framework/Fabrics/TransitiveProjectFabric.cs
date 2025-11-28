// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Fabrics
{
    /// <summary>
    /// A compile-time entry point that executes within the compiler and IDE to add aspects, configure libraries, and implement architecture rules
    /// for projects that reference the assembly containing this fabric. The fabric does not execute in the project where it is defined.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Transitive project fabrics enable aspect library authors to automatically apply aspects and configuration to consuming projects.
    /// Unlike <see cref="ProjectFabric"/>, which only affects the project in which it is defined, a <see cref="TransitiveProjectFabric"/>
    /// applies transformations to any project that references the assembly containing the fabric.
    /// </para>
    /// <para>
    /// The <see cref="ProjectFabric.AmendProject"/> method is not executed in the current project where the fabric is defined.
    /// If you need to apply transformations to both the current project and referencing projects, create two separate fabric classes:
    /// one inheriting from <see cref="ProjectFabric"/> and another from <see cref="TransitiveProjectFabric"/>.
    /// </para>
    /// <para>
    /// When multiple transitive project fabrics are present in the dependency graph, they are executed in the following order:
    /// </para>
    /// <list type="number">
    /// <item><description>Fabrics that are deepest in the dependency graph are executed first.</description></item>
    /// <item><description>Fabrics are then ordered by assembly name.</description></item>
    /// <item><description>Fabrics are ordered by source file location (closest to the root directory first).</description></item>
    /// <item><description>Finally, fabrics are ordered by type name.</description></item>
    /// </list>
    /// <para>
    /// Transitive project fabrics are commonly used to expose configuration APIs that can be consumed from fabrics in referencing projects.
    /// </para>
    /// </remarks>
    /// <seealso cref="ProjectFabric"/>
    /// <seealso href="@fabrics"/>
    /// <seealso href="@exposing-options"/>
    /// <seealso href="@aspect-configuration"/>
    /// <seealso href="@fabrics-adding-aspects"/>
    public abstract class TransitiveProjectFabric : ProjectFabric;
}