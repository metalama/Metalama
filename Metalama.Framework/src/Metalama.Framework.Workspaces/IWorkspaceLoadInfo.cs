// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Utilities;
using System.Collections.Immutable;

namespace Metalama.Framework.Workspaces
{
    /// <summary>
    /// Exposes the information needed to reconstruct a <see cref="Workspace"/>.
    /// </summary>
    /// <seealso cref="Workspace"/>
    /// <seealso cref="WorkspaceCollection"/>
    [Hidden]
    [PublicAPI]
    public interface IWorkspaceLoadInfo
    {
        /// <summary>
        /// Gets the paths of the projects or solutions that were loaded into the workspace.
        /// </summary>
        ImmutableArray<string> LoadedPaths { get; }

        /// <summary>
        /// Gets the MSBuild properties that were used when loading the workspace.
        /// </summary>
        ImmutableDictionary<string, string> Properties { get; }
    }
}