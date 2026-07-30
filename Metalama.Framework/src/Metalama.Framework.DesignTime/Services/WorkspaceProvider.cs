// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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

    /// <summary>
    /// Gets the workspace, or <c>null</c> when this host has none.
    /// </summary>
    /// <remarks>
    /// Nullable rather than throwing, because having no workspace is a normal state of a supported host and not an
    /// error. Metalama running as a plain analyzer, which is what it does in an IDE without the Metalama extension, has
    /// no way to reach one. Expressing that as <see cref="NotSupportedException"/> meant every caller that forgot to
    /// catch it turned a degraded-but-working situation into an exception out of the source generator, which costs the
    /// project all of its design-time support. See #1749.
    /// </remarks>
    protected abstract Task<Workspace?> GetWorkspaceAsync( CancellationToken cancellationToken = default );

    internal bool TryGetWorkspace( [NotNullWhen( true )] out Workspace? workspace )
    {
        var task = this.GetWorkspaceAsync();

        if ( task.IsCompleted )
        {
#pragma warning disable VSTHRD002
            workspace = task.Result;
#pragma warning restore VSTHRD002

            return workspace != null;
        }
        else
        {
            workspace = null;

            return false;
        }
    }

    /// <summary>
    /// Gets the project of a given <see cref="ProjectKey"/>, or <c>null</c> when there is no workspace or no project
    /// matches. Throws when several projects share the key.
    /// </summary>
    /// <remarks>
    /// A key must identify a project, and since Metalama 2026.1 it does, because the MSBuild targets define a
    /// <c>METALAMA_PROJECT_&lt;hash&gt;</c> compilation symbol derived from the project path, target framework,
    /// configuration and platform. Several projects under one key therefore mean a broken configuration, and returning
    /// an arbitrary match would hand the caller another project's work with no way to notice. See #1749.
    /// </remarks>
    public async ValueTask<Microsoft.CodeAnalysis.Project?> GetProjectAsync( ProjectKey projectKey, CancellationToken cancellationToken )
    {
        var workspace = await this.GetWorkspaceAsync( cancellationToken );

        if ( workspace == null )
        {
            this.Logger.Trace?.Log( $"Cannot find a project for '{projectKey}': this host has no workspace." );

            return null;
        }

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

        // The whole candidate set is collected before anything is cached or returned. Caching as we went would defeat
        // the ambiguity check below, because the fast path above would then serve the arbitrary first match on every
        // later call. Only projects whose assembly name matches can share the key, so this set is small.
        var candidatesByKey = new Dictionary<ProjectKey, List<Microsoft.CodeAnalysis.Project>>();

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

            if ( !candidatesByKey.TryGetValue( thisProjectKey, out var candidates ) )
            {
                candidates = [];
                candidatesByKey[thisProjectKey] = candidates;
            }

            candidates.Add( project );
        }

        foreach ( var pair in candidatesByKey )
        {
            if ( pair.Value.Count == 1 )
            {
                this._projectKeyToProjectIdMap.TryAdd( pair.Key, pair.Value[0].Id );
            }
        }

        if ( !candidatesByKey.TryGetValue( projectKey, out var matches ) )
        {
            // Error: the compilation could not be found.
            this.Logger.Warning?.Log( $"Cannot find a project in the workspace for '{projectKey}'." );

            return null;
        }

        if ( matches.Count > 1 )
        {
            throw new InvalidOperationException(
                $"{matches.Count} projects in the workspace have the same Metalama project key '{projectKey}': "
                + $"{string.Join( ", ", matches.Select( p => p.FilePath ?? p.Name ) )}. A project key is an assembly name and a hash of the "
                + "compilation symbols, and it must identify a project uniquely. Check that the METALAMA_PROJECT_* compilation symbol defined by "
                + "Metalama.Framework.targets is present in these projects and has not been removed by a DefineConstants assignment." );
        }

        return matches[0];
    }

    public async ValueTask<Compilation?> GetCompilationAsync( ProjectKey projectKey, CancellationToken cancellationToken = default )
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