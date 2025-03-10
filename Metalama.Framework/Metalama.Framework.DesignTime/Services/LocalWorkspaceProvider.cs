// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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