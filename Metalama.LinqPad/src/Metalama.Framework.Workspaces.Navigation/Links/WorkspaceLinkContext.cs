// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Workspaces.Navigation.Facade;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Metalama.Framework.Workspaces.Navigation.Links;

/// <summary>
/// An <see cref="ILinkContext"/> over workspaces loaded by a <see cref="WorkspaceCollection"/> and named by keys.
/// </summary>
internal sealed class WorkspaceLinkContext : ILinkContext
{
    /// <summary>
    /// The collection anchors and the property of <see cref="Workspace"/> or <see cref="Project"/> that each one resolves to.
    /// </summary>
    private static readonly ImmutableDictionary<string, string> _collectionAnchors = new Dictionary<string, string>
    {
        ["projects"] = nameof(Workspace.Projects),
        ["diagnostics"] = nameof(Workspace.Diagnostics),
        ["aspect-classes"] = nameof(Workspace.AspectClasses),
        ["aspect-instances"] = nameof(Workspace.AspectInstances),
        ["aspect-layers"] = nameof(Workspace.AspectLayers),
        ["advice"] = nameof(Workspace.Advice),
        ["transformations"] = nameof(Workspace.Transformations)
    }.ToImmutableDictionary( StringComparer.Ordinal );

    private readonly WorkspaceCollection _collection;
    private readonly IReadOnlyDictionary<string, Workspace> _workspaces;
    private readonly IReadOnlyDictionary<string, string> _fingerprints;
    private readonly string? _defaultWorkspace;

    public WorkspaceLinkContext(
        WorkspaceCollection collection,
        IReadOnlyDictionary<string, Workspace> workspaces,
        IReadOnlyDictionary<string, string> fingerprints,
        string? defaultWorkspace )
    {
        this._collection = collection;
        this._workspaces = workspaces;
        this._fingerprints = fingerprints;
        this._defaultWorkspace = defaultWorkspace ?? (workspaces.Count == 1 ? workspaces.Keys.Single() : null);
    }

    public static IReadOnlyList<string> CollectionAnchorNames { get; } = [.._collectionAnchors.Keys.Append( "project" ).Append( "workspace" ).OrderBy( k => k, StringComparer.Ordinal )];

    public static string GetScope( Project project )
        => string.IsNullOrEmpty( project.TargetFramework ) ? project.Name : $"{project.Name}@{project.TargetFramework}";

    /// <summary>
    /// Gets the default base of a workspace: its only project and target framework when it has one, otherwise the workspace
    /// alone.
    /// </summary>
    public static LinkBase GetDefaultBase( string key, Workspace workspace )
        => new( key, workspace.Projects.Length == 1 ? GetScope( workspace.Projects[0] ) : null );

    public bool TryGetScope( ICompilation compilation, out string? workspace, [NotNullWhen( true )] out string? scope, out bool isTransformed )
    {
        workspace = null;
        scope = null;
        isTransformed = false;

        if ( !this._collection.TryFindProject( compilation.GetRoslynCompilation(), out var foundWorkspace, out var project, out isTransformed ) )
        {
            return false;
        }

        workspace = this.GetKey( foundWorkspace );
        scope = GetScope( project );

        return true;
    }

    public Link? GetObjectAnchor( object value )
    {
        switch ( value )
        {
            case Project project:
                {
                    var workspace = this._workspaces.FirstOrDefault( p => p.Value.Projects.Contains( project ) ).Key;

                    return workspace == null ? null : new Link( workspace, GetScope( project ), false, "project", [], this.GetFingerprint( workspace ) );
                }

            case Workspace workspace:
                {
                    var key = this.GetKey( workspace );

                    return key == null ? null : new Link( key, Link.WholeWorkspaceScope, false, "workspace", [], this.GetFingerprint( key ) );
                }

            default:
                return null;
        }
    }

    public string? GetFingerprint( string? workspace )
    {
        workspace ??= this._defaultWorkspace;

        return workspace != null && this._fingerprints.TryGetValue( workspace, out var fingerprint ) ? fingerprint : null;
    }

    public bool TryGetCompilation(
        string? workspace,
        string scope,
        bool isTransformed,
        [NotNullWhen( true )] out ICompilation? compilation,
        [NotNullWhen( false )] out string? error )
    {
        compilation = null;

        if ( !this.TryGetProject( workspace, scope, out var project, out error ) )
        {
            return false;
        }

        compilation = (isTransformed ? project.TransformedCode : project.SourceCode).Compilations[0];

        return true;
    }

    public bool TryGetCollectionAnchor( string? workspace, string scope, string name, out object? value, [NotNullWhen( false )] out string? error )
    {
        value = null;

        if ( !this.TryGetWorkspace( workspace, out var foundWorkspace, out error ) )
        {
            return false;
        }

        object owner;

        if ( scope == Link.WholeWorkspaceScope )
        {
            if ( name == "workspace" )
            {
                value = foundWorkspace;

                return true;
            }

            owner = foundWorkspace;
        }
        else
        {
            if ( !this.TryGetProject( workspace, scope, out var project, out error ) )
            {
                return false;
            }

            if ( name == "project" )
            {
                value = project;

                return true;
            }

            if ( name == "projects" )
            {
                value = ImmutableArray.Create( project );

                return true;
            }

            owner = project;
        }

        if ( !_collectionAnchors.TryGetValue( name, out var propertyName ) )
        {
            error = $"Unknown anchor '{name}'. Use a declaration or type identifier, or one of: {string.Join( ", ", CollectionAnchorNames )}.";

            return false;
        }

        if ( !FacadeTypeFactory.Default.GetFacadeType( owner.GetType() ).TryGetProperty( propertyName, out var property ) )
        {
            error = $"The anchor '{name}' is not available on {owner.GetType().Name}.";

            return false;
        }

        value = property.GetFunc( owner );

        return true;
    }

    private string? GetKey( Workspace workspace ) => this._workspaces.FirstOrDefault( p => ReferenceEquals( p.Value, workspace ) ).Key;

    private bool TryGetWorkspace( string? key, [NotNullWhen( true )] out Workspace? workspace, [NotNullWhen( false )] out string? error )
    {
        key ??= this._defaultWorkspace;

        if ( key == null )
        {
            workspace = null;

            error = this._workspaces.Count == 0
                ? "No workspace is loaded. Call load_workspace first."
                : $"Several workspaces are loaded, so the link must name one of: {string.Join( ", ", this._workspaces.Keys )}.";

            return false;
        }

        if ( !this._workspaces.TryGetValue( key, out workspace ) )
        {
            error = $"The workspace '{key}' is not loaded. Loaded workspaces: {string.Join( ", ", this._workspaces.Keys )}.";

            return false;
        }

        error = null;

        return true;
    }

    private bool TryGetProject( string? workspaceKey, string scope, [NotNullWhen( true )] out Project? project, [NotNullWhen( false )] out string? error )
    {
        project = null;

        if ( !this.TryGetWorkspace( workspaceKey, out var workspace, out error ) )
        {
            return false;
        }

        if ( scope == Link.WholeWorkspaceScope )
        {
            error = "This anchor requires a project scope (<project>@<tfm>), not the whole workspace (~).";

            return false;
        }

        var at = scope.LastIndexOf( '@' );
        var name = at < 0 ? scope : scope.Substring( 0, at );
        var targetFramework = at < 0 ? null : scope.Substring( at + 1 );

        var candidates = workspace.Projects
            .Where(
                p => string.Equals( p.Name, name, StringComparison.OrdinalIgnoreCase )
                     && (targetFramework == null || string.Equals( p.TargetFramework, targetFramework, StringComparison.OrdinalIgnoreCase )) )
            .ToList();

        switch ( candidates.Count )
        {
            case 1:
                project = candidates[0];

                return true;

            case 0:
                error = $"The workspace has no project '{scope}'. Projects: {string.Join( ", ", workspace.Projects.Select( GetScope ) )}.";

                return false;

            default:
                error = $"The scope '{scope}' is ambiguous. Use one of: {string.Join( ", ", candidates.Select( GetScope ) )}.";

                return false;
        }
    }
}
