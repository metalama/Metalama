// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Fabrics
{
    /// <summary>
    /// A class that, when inherited by a type in a project (under any name or namespace), allows that type to analyze and
    /// add aspects to that project.
    /// </summary>
    /// <remarks>
    /// When the project contains several project fabrics, the ones whose source file is the closest to the root directory is executed
    /// first. The project fabrics are then ordered by type name.
    /// </remarks>
    /// <seealso href="@fabrics"/> 
    /// <seealso href="@aspect-configuration"/>
    /// <seealso href="@fabrics-adding-aspects"/>
    public abstract class ProjectFabric : Fabric
    {
        /// <summary>
        /// The user can implement this method to analyze types in the current project, add aspects, and report or suppress diagnostics.
        /// </summary>
        public abstract void AmendProject( IProjectAmender amender );
    }
}