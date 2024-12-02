// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Diagnostics;
using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Caching;
using Metalama.Framework.Services;
using Microsoft.CodeAnalysis;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.DesignTime.Services;

public abstract class WorkspaceProvider : IGlobalService, IDisposable
{
    private readonly TimeBasedCache<ProjectKey, ProjectId> _projectKeyToProjectIdMap = new( TimeSpan.FromMinutes( 10 ) );

    protected ILogger Logger { get; }

    protected WorkspaceProvider( GlobalServiceProvider serviceProvider )
    {
        this.Logger = serviceProvider.GetLoggerFactory().GetLogger( "WorkspaceProvider" );
    }

    protected abstract Task<Workspace> GetWorkspaceAsync( CancellationToken cancellationToken = default );

    internal bool TryGetWorkspace( [NotNullWhen( true )] out Workspace? workspace )
    {
        var task = this.GetWorkspaceAsync();

        if ( task.IsCompleted )
        {
#pragma warning disable VSTHRD002
            workspace = task.Result;
#pragma warning restore VSTHRD002

            return true;
        }
        else
        {
            workspace = null;

            return false;
        }
    }

    internal async ValueTask<Microsoft.CodeAnalysis.Project?> GetProjectAsync( ProjectKey projectKey, CancellationToken cancellationToken )
    {
        var workspace = await this.GetWorkspaceAsync( cancellationToken );

        if ( this._projectKeyToProjectIdMap.TryGetValue( projectKey, out var projectId ) )
        {
            var project = workspace.CurrentSolution.GetProject( projectId );

            if ( project != null )
            {
                return project;
            }

            // When a project is unloaded and reloaded, its ID changes, so we need to remove the old ID from the cache.
            this._projectKeyToProjectIdMap.TryRemove( projectKey );
        }

        foreach ( var project in workspace.CurrentSolution.Projects )
        {
            cancellationToken.ThrowIfCancellationRequested();

            if ( project.AssemblyName != projectKey.AssemblyName )
            {
                continue;
            }

            var thisProjectKey = ProjectKeyFactory.FromProject( project );

            if ( thisProjectKey == null )
            {
                // This is not a C# project.
                continue;
            }

            this._projectKeyToProjectIdMap.TryAdd( thisProjectKey, project.Id );

            if ( thisProjectKey == projectKey )
            {
                return project;
            }
        }

        // Error: the compilation could not be found.
        this.Logger.Warning?.Log( $"Cannot find a project in the workspace for '{projectKey}'." );

        return default;
    }

    internal async ValueTask<Compilation?> GetCompilationAsync( ProjectKey projectKey, CancellationToken cancellationToken = default )
    {
        var project = await this.GetProjectAsync( projectKey, cancellationToken );

        if ( project == null )
        {
            return null;
        }

        if ( !project.TryGetCompilation( out var compilation ) )
        {
            compilation = await project.GetCompilationAsync( cancellationToken );
        }

        return compilation;
    }

    public virtual void Dispose()
    {
        this._projectKeyToProjectIdMap.Dispose();
    }
}