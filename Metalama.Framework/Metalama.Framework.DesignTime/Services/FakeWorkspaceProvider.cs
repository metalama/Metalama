// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.DesignTime.Services;

internal sealed class FakeWorkspaceProvider : WorkspaceProvider
{
    public FakeWorkspaceProvider( GlobalServiceProvider serviceProvider ) : base( serviceProvider ) { }

    protected override Task<Workspace> GetWorkspaceAsync( CancellationToken cancellationToken = default ) => throw new NotSupportedException();
}