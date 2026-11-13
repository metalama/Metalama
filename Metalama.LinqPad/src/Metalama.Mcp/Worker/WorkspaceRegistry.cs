// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Workspaces;
using Metalama.Framework.Workspaces.Navigation.Links;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Mcp.Worker;

/// <summary>
/// The workspaces loaded in the worker, by key. The key is the loaded paths relative to the working directory.
/// </summary>
internal sealed class WorkspaceRegistry
{
    private readonly Dictionary<string, Entry> _entries = new( StringComparer.Ordinal );

    public sealed record Entry( string Key, Workspace Workspace, string Fingerprint, string RootDirectory )
    {
        public LinkBase DefaultBase => WorkspaceLinkContext.GetDefaultBase( this.Key, this.Workspace );
    }

    public IReadOnlyCollection<Entry> Entries => this._entries.Values;

    public static string GetKey( IEnumerable<string> fullPaths )
        => string.Join( "+", fullPaths.Select( p => Path.GetRelativePath( Environment.CurrentDirectory, p ).Replace( '\\', '/' ) ) );

    public async Task<Entry> LoadAsync( IReadOnlyList<string> paths, ImmutableDictionary<string, string>? properties, CancellationToken cancellationToken )
    {
        var fullPaths = paths.Select( p => Path.GetFullPath( p ) ).ToImmutableArray();
        var key = GetKey( fullPaths );

        var workspace = await WorkspaceCollection.Default.LoadAsync( fullPaths, properties, restore: true, cancellationToken );

        var entry = new Entry( key, workspace, WorkspaceFingerprint.Compute( workspace, NewLoadGeneration() ), Path.GetDirectoryName( fullPaths[0] )! );
        this._entries[key] = entry;

        return entry;
    }

    public async Task<Entry> ReloadAsync( Entry entry, CancellationToken cancellationToken )
    {
        var workspace = await entry.Workspace.ReloadAsync( restore: true, cancellationToken );
        var reloaded = entry with { Workspace = workspace, Fingerprint = WorkspaceFingerprint.Compute( workspace, NewLoadGeneration() ) };
        this._entries[entry.Key] = reloaded;

        return reloaded;
    }

    /// <summary>
    /// Identifies a load, including across restarts of the worker, so that an index link never survives a reload. See
    /// <see cref="WorkspaceFingerprint"/>.
    /// </summary>
    private static long NewLoadGeneration() => Random.Shared.NextInt64();

    public void Unload( Entry entry )
    {
        this._entries.Remove( entry.Key );
        entry.Workspace.Dispose();
    }

    /// <summary>
    /// Gets a workspace by key, or the only loaded one when the key is <c>null</c>.
    /// </summary>
    public bool TryGet( string? key, [NotNullWhen( true )] out Entry? entry, [NotNullWhen( false )] out string? error )
    {
        if ( key == null )
        {
            switch ( this._entries.Count )
            {
                case 1:
                    entry = this._entries.Values.Single();
                    error = null;

                    return true;

                case 0:
                    entry = null;
                    error = "No workspace is loaded. Call load_workspace first.";

                    return false;

                default:
                    entry = null;
                    error = $"Several workspaces are loaded; pass `workspace` with one of: {string.Join( ", ", this._entries.Keys )}.";

                    return false;
            }
        }

        if ( this._entries.TryGetValue( key, out entry ) )
        {
            error = null;

            return true;
        }

        error = $"The workspace '{key}' is not loaded. Loaded workspaces: {(this._entries.Count == 0 ? "none" : string.Join( ", ", this._entries.Keys ))}.";

        return false;
    }

    public WorkspaceLinkContext CreateLinkContext( string? defaultWorkspace )
        => new(
            WorkspaceCollection.Default,
            this._entries.ToDictionary( e => e.Key, e => e.Value.Workspace, StringComparer.Ordinal ),
            this._entries.ToDictionary( e => e.Key, e => e.Value.Fingerprint, StringComparer.Ordinal ),
            defaultWorkspace );
}
