// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Threading;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.DesignTime.Services;

/// <summary>
/// An implementation of <see cref="WorkspaceProvider"/> that expects the UI services to be in the same process and to call <see cref="TrySetWorkspace"/>.
/// </summary>
public sealed class LocalWorkspaceProvider : WorkspaceProvider
{
    private readonly TaskCompletionSource<Workspace> _workspace = new();

    public LocalWorkspaceProvider( GlobalServiceProvider serviceProvider ) : base( serviceProvider ) { }

    protected override Task<Workspace> GetWorkspaceAsync( CancellationToken cancellationToken = default )
    {
        if ( !this._workspace.Task.IsCompleted )
        {
            this.Logger.Warning?.Log( $"The workspace is not yet available. Waiting." );
        }

        return this._workspace.Task.WithCancellation( cancellationToken );
    }

    public bool TrySetWorkspace( Workspace workspace ) => this._workspace.TrySetResult( workspace );
}