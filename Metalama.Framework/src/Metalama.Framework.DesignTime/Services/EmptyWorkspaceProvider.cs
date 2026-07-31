// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.DesignTime.Services;

/// <summary>
/// An implementation of <see cref="WorkspaceProvider"/> for a host that has no workspace, which is the situation when
/// Metalama runs as a plain analyzer.
/// </summary>
/// <remarks>
/// This situation occurs in an integrated development environment in which the Metalama extension is not installed, and
/// it is therefore a supported one. This implementation returns <c>null</c> instead of throwing an exception. A previous
/// implementation threw a <see cref="NotSupportedException"/>, which propagated out of the source generator and disabled
/// every design-time feature of the project instead of the single feature that requires a workspace. See issue #1749.
/// </remarks>
internal sealed class EmptyWorkspaceProvider : WorkspaceProvider
{
    public EmptyWorkspaceProvider( GlobalServiceProvider serviceProvider ) : base( serviceProvider ) { }

    protected override Task<Workspace?> GetWorkspaceAsync( CancellationToken cancellationToken = default ) => Task.FromResult<Workspace?>( null );
}