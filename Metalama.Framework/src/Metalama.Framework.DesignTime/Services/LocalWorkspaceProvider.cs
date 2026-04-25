// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Contracts.Workspaces;
using Metalama.Framework.DesignTime.Extensibility;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Threading;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.DesignTime.Services;

/// <summary>
/// An implementation of <see cref="WorkspaceProvider"/> that expects the UI services to be in the same process and to call <see cref="TrySetWorkspace"/>.
/// </summary>
public sealed class LocalWorkspaceProvider : WorkspaceProvider, IWorkspaceReceiver
{
    private readonly TaskCompletionSource<Workspace> _workspace = new();
    private readonly IProjectOptionsFactory? _projectOptionsFactory;
    private readonly DesignTimeExtensionManager? _designTimeExtensionManager;

    public LocalWorkspaceProvider( GlobalServiceProvider serviceProvider ) : base( serviceProvider )
    {
        // These services may be absent in tests.
        this._projectOptionsFactory = serviceProvider.GetService<IProjectOptionsFactory>();
        this._designTimeExtensionManager = serviceProvider.GetService<DesignTimeExtensionManager>();
    }

    protected override Task<Workspace> GetWorkspaceAsync( CancellationToken cancellationToken = default )
    {
        if ( !this._workspace.Task.IsCompleted )
        {
            this.Logger.Warning?.Log( $"The workspace is not yet available. Waiting." );
        }

        return this._workspace.Task.WithCancellation( cancellationToken );
    }

    public bool TrySetWorkspace( Workspace workspace )
    {
        if ( !this._workspace.TrySetResult( workspace ) )
        {
            return false;
        }

        // When we have new projects, load their extension immediately.
        // Other entry points call DesignTimeExtensionManager.OnProjectDiscovered, but they are called by the
        // IDE on demand. We want to make sure they extensions are immediately available.

#if ROSLYN_5_0_0_OR_GREATER
        workspace.RegisterWorkspaceChangedHandler( this.OnWorkspaceChanged );
#else
        workspace.WorkspaceChanged += ( _, args ) => this.OnWorkspaceChanged( args );
#endif

        this.OnProjectsDiscovered( workspace.CurrentSolution.Projects );

        return true;
    }

    private void OnProjectsDiscovered( IEnumerable<Microsoft.CodeAnalysis.Project> projects )
    {
        if ( this._projectOptionsFactory == null || this._designTimeExtensionManager == null )
        {
            return;
        }

        foreach ( var project in projects )
        {
            var projectOptions = this._projectOptionsFactory.GetProjectOptions( project );
            this._designTimeExtensionManager.OnProjectDiscovered( projectOptions );
        }
    }

    private void OnWorkspaceChanged( WorkspaceChangeEventArgs args )
    {
        if ( args == null! )
        {
            return;
        }

        switch ( args.Kind )
        {
            case WorkspaceChangeKind.SolutionAdded or WorkspaceChangeKind.SolutionChanged or WorkspaceChangeKind.SolutionReloaded:
                this.OnProjectsDiscovered( args.NewSolution.Projects );

                break;

            case WorkspaceChangeKind.ProjectAdded or WorkspaceChangeKind.ProjectChanged or WorkspaceChangeKind.ProjectReloaded when args.ProjectId != null:
                var project = args.NewSolution.GetProject( args.ProjectId );

                if ( project != null )
                {
                    this.OnProjectsDiscovered( [project] );
                }

                break;
        }
    }

    void IWorkspaceReceiver.SetWorkspace( Workspace workspace ) => this.TrySetWorkspace( workspace );
}