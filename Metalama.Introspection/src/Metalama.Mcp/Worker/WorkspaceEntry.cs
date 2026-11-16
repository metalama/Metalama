// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Workspaces;
using Metalama.Introspection.Navigation.Links;

namespace Metalama.Mcp.Worker;

/// <summary>
/// A workspace loaded in the worker.
/// </summary>
/// <param name="Key">The key of the workspace: its loaded paths relative to the working directory.</param>
/// <param name="Workspace">The workspace.</param>
/// <param name="Fingerprint">The fingerprint of this load, which fragile links carry.</param>
/// <param name="RootDirectory">The directory against which source locations are made relative.</param>
internal sealed record WorkspaceEntry( string Key, Workspace Workspace, string Fingerprint, string RootDirectory )
{
    public LinkBase DefaultBase => WorkspaceLinkContext.GetDefaultBase( this.Key, this.Workspace );
}
