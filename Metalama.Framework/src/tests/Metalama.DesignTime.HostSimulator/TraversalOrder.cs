// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.DesignTime.HostSimulator;

/// <summary>
/// The order in which the projects of a solution are analyzed.
/// </summary>
public enum TraversalOrder
{
    /// <summary>
    /// The order in which the solution declares the projects.
    /// </summary>
    Solution,

    /// <summary>
    /// The dependency order: a project is analyzed after the projects it references.
    /// </summary>
    /// <remarks>
    /// This is the favourable order, and the one a batch build uses. Every upstream pipeline has already produced
    /// its configuration by the time a downstream pipeline asks for it.
    /// </remarks>
    Graph,

    /// <summary>
    /// The reverse dependency order: a project is analyzed before the projects it references.
    /// </summary>
    /// <remarks>
    /// This is the adverse order, and an editor produces it routinely, because it analyzes the document the user
    /// opened before anything that document depends on. A downstream pipeline then asks for an upstream
    /// configuration that does not exist yet and has to build its own projection of the upstream instead.
    /// </remarks>
    Reverse
}
