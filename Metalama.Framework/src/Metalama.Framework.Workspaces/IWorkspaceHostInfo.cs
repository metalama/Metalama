// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Utilities;
using SharpCrafters.Backstage.Telemetry;

namespace Metalama.Framework.Workspaces
{
    /// <summary>
    /// Exposes the information that a host of the introspection API, such as Metalama.LinqPad or Metalama.Mcp, needs to report
    /// the usage and the exceptions of a <see cref="Workspace"/>. The members are not meant for queries, so <see cref="Workspace"/>
    /// implements this interface explicitly.
    /// </summary>
    /// <seealso cref="Workspace"/>
    /// <seealso cref="WorkspaceCollection.WorkspaceLoaded"/>
    [Hidden]
    [PublicAPI]
    public interface IWorkspaceHostInfo
    {
        /// <summary>
        /// Gets a key that identifies the workspace in its <see cref="WorkspaceCollection"/>. The key is derived from the loaded
        /// paths and the MSBuild properties.
        /// </summary>
        string Key { get; }

        /// <summary>
        /// Gets the telemetry context that the workspace opened when it was loaded, or <see langword="null"/> when the telemetry
        /// service is not available.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The context decides whether telemetry is collected: the consent of the user, the opt-out of the repository
        /// (<c>metalama.json</c>), and the gates of the process. A host uses it for the usage session of the workspace and for the
        /// exceptions of the workspace. The service provider of each project of the workspace exposes the same context to the
        /// engine. A reload of the workspace opens a new context.
        /// </para>
        /// <para>
        /// The policy of the context is resolved from the directory of the first loaded path. When the workspace contains a
        /// project of another repository, the <c>metalama.json</c> file of that repository is not consulted.
        /// </para>
        /// </remarks>
        ITelemetryContext? TelemetryContext { get; }
    }
}
