// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Fabrics
{
    /// <summary>
    /// A class that, when inherited by a type in an assembly (under any name or namespace), allows that type to analyze and
    /// add aspects to any project that references this assembly. However, the <see cref="ProjectFabric.AmendProject"/> method
    /// is not executed in the current project (you will need another class that does not implement <see cref="TransitiveProjectFabric"/>
    /// to amend the current project). 
    /// </summary>
    /// <remarks>
    /// When the project contains several transitive project fabrics, the ones that are the deepest in the dependency graph are
    /// executed first. Then, transitive fabrics are ordered by assembly name, then by distance to the root directory in the file system,
    /// then by type name.
    /// </remarks>
    /// <seealso href="@fabrics"/> 
    /// <seealso href="@exposing-configuration"/>
    /// <seealso href="@fabrics-adding-aspects"/>
    public abstract class TransitiveProjectFabric : ProjectFabric;
}