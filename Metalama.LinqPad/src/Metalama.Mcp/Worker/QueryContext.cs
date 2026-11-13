// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Workspaces;
using Metalama.Framework.Workspaces.Navigation.Links;
using System.Collections.Generic;
using System.Threading;

namespace Metalama.Mcp.Worker;

/// <summary>
/// What a query sees: the workspaces, the link base and resolver, and its cancellation token.
/// </summary>
internal sealed record QueryContext(
    Workspace? Workspace,
    IReadOnlyDictionary<string, Workspace> Workspaces,
    LinkBase? Base,
    LinkResolver Resolver,
    CancellationToken CancellationToken );
