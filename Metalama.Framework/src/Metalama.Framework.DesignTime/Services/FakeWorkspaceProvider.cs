// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.DesignTime.Services;

/// <summary>
/// The <see cref="WorkspaceProvider"/> of a host that has no workspace, i.e. Metalama running as a plain analyzer.
/// </summary>
/// <remarks>
/// That is the normal situation in an IDE without the Metalama extension, so it returns <c>null</c> rather than
/// throwing. It used to throw <see cref="NotSupportedException"/>, which travelled out of the source generator and cost
/// the project every design-time feature instead of the one feature that needs a workspace. See #1749.
/// </remarks>
internal sealed class FakeWorkspaceProvider : WorkspaceProvider
{
    public FakeWorkspaceProvider( GlobalServiceProvider serviceProvider ) : base( serviceProvider ) { }

    protected override Task<Workspace?> GetWorkspaceAsync( CancellationToken cancellationToken = default ) => Task.FromResult<Workspace?>( null );
}