// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Metalama.Framework.Workspaces;

/// <summary>
/// Restores the severity of the NuGet diagnostics that <c>MSBuildWorkspace</c> reports when it loads a project.
/// </summary>
/// <remarks>
/// The design-time build re-emits the messages stored in the <c>logs</c> section of <c>project.assets.json</c>, for instance the
/// NuGet audit warnings NU1901 to NU1904. <c>MSBuildWorkspace</c> reports every MSBuild error and warning as a
/// <see cref="WorkspaceDiagnosticKind.Failure"/>, and the message it reports does not include the original severity.
/// This class reads the level of each message from the assets file and reports the messages that are not errors as
/// <see cref="WorkspaceDiagnosticKind.Warning"/>, as <c>dotnet build</c> does.
/// </remarks>
internal sealed class NuGetAssetsDiagnostics
{
    private readonly ConcurrentDictionary<string, bool> _assetsFiles = new( StringComparer.OrdinalIgnoreCase );
    private readonly ConcurrentDictionary<string, bool> _nonErrorMessages = new( StringComparer.Ordinal );

    /// <summary>
    /// Reads the messages of an assets file. The method can be called concurrently and several times for the same file.
    /// </summary>
    public void AddAssetsFile( string? path )
    {
        if ( string.IsNullOrEmpty( path ) || !this._assetsFiles.TryAdd( path, true ) || !File.Exists( path ) )
        {
            return;
        }

        try
        {
            using var stream = File.OpenRead( path );
            using var document = JsonDocument.Parse( stream );

            if ( !document.RootElement.TryGetProperty( "logs", out var logs ) || logs.ValueKind != JsonValueKind.Array )
            {
                return;
            }

            foreach ( var log in logs.EnumerateArray() )
            {
                if ( log.TryGetProperty( "level", out var level )
                     && !string.Equals( level.GetString(), "Error", StringComparison.OrdinalIgnoreCase )
                     && log.TryGetProperty( "message", out var message )
                     && message.GetString() is { Length: > 0 } messageText )
                {
                    this._nonErrorMessages.TryAdd( messageText, true );
                }
            }
        }
        catch ( Exception e ) when ( e is IOException or UnauthorizedAccessException or JsonException )
        {
            // A missing or unreadable assets file only means that we cannot correct the severity.
        }
    }

    /// <summary>
    /// Returns the diagnostics where the failures that report a NuGet message that is not an error are replaced by warnings.
    /// </summary>
    public ImmutableList<WorkspaceDiagnostic> Fix( IEnumerable<WorkspaceDiagnostic> diagnostics )
        => diagnostics.Select( d => d.Kind == WorkspaceDiagnosticKind.Failure && this.IsNonErrorMessage( d.Message )
                                   ? new WorkspaceDiagnostic( WorkspaceDiagnosticKind.Warning, d.Message )
                                   : d )
            .ToImmutableList();

    // MSBuildWorkspace formats the message as "Msbuild failed when processing the file '<project>' with message: <file>: (<line>, <column>): <message>".
    private bool IsNonErrorMessage( string diagnosticMessage )
        => this._nonErrorMessages.Keys.Any( m => diagnosticMessage.EndsWith( ": " + m, StringComparison.Ordinal ) );
}
