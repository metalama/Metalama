// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Workspaces;
using Metalama.Introspection.Navigation.Links;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.IO.Hashing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Mcp.Worker;

/// <summary>
/// The workspaces loaded in the worker, by key.
/// </summary>
internal sealed class WorkspaceRegistry
{
    private static readonly StringComparer _pathComparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    private readonly Dictionary<string, WorkspaceEntry> _entries = new( StringComparer.Ordinal );

    public IReadOnlyCollection<WorkspaceEntry> Entries => this._entries.Values;

    /// <summary>
    /// Gets the key of a workspace, which is the first segment of its links: the file names of the loaded paths, joined with
    /// <c>+</c>, such as <c>App.sln</c>. Directories are left out, so that links need no escaped separators. When the name is
    /// already taken by other paths, a hash of the full paths is appended, such as <c>App.sln~3f9a1c</c>.
    /// </summary>
    /// <param name="fullPaths">The full paths to load.</param>
    /// <param name="loaded">The full paths of the loaded workspaces, by key.</param>
    public static string GetKey( IReadOnlyList<string> fullPaths, IReadOnlyDictionary<string, ImmutableArray<string>> loaded )
    {
        var key = string.Join( "+", fullPaths.Select( Path.GetFileName ) );

        if ( !loaded.TryGetValue( key, out var paths ) || paths.SequenceEqual( fullPaths, _pathComparer ) )
        {
            return key;
        }

        var normalized = string.Join( "|", fullPaths.Select( p => OperatingSystem.IsWindows() ? p.ToUpperInvariant() : p ) );
        var hash = XxHash64.HashToUInt64( Encoding.UTF8.GetBytes( normalized ) ).ToString( "x16", CultureInfo.InvariantCulture ).Substring( 0, 6 );

        return $"{key}~{hash}";
    }

    public async Task<WorkspaceEntry> LoadAsync( IReadOnlyList<string> paths, ImmutableDictionary<string, string>? properties, CancellationToken cancellationToken )
    {
        var fullPaths = paths.Select( p => Path.GetFullPath( p ) ).ToImmutableArray();
        var key = GetKey( fullPaths, this._entries.ToDictionary( e => e.Key, e => e.Value.Paths, StringComparer.Ordinal ) );

        var workspace = await WorkspaceCollection.Default.LoadAsync( fullPaths, properties, restore: true, cancellationToken );

        var entry = new WorkspaceEntry(
            key,
            fullPaths,
            workspace,
            WorkspaceFingerprint.Compute( workspace, NewLoadGeneration() ),
            Path.GetDirectoryName( fullPaths[0] )! );
        this._entries[key] = entry;

        return entry;
    }

    public async Task<WorkspaceEntry> ReloadAsync( WorkspaceEntry entry, CancellationToken cancellationToken )
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

    public void Unload( WorkspaceEntry entry )
    {
        this._entries.Remove( entry.Key );
        entry.Workspace.Dispose();
    }

    /// <summary>
    /// Gets a workspace by key, or the only loaded one when the key is <c>null</c>.
    /// </summary>
    public bool TryGet( string? key, [NotNullWhen( true )] out WorkspaceEntry? entry, [NotNullWhen( false )] out string? error )
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
