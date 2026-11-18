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
/// This class reads the level of each message from the assets file of the project that reported it, and reports the messages
/// that are not errors as <see cref="WorkspaceDiagnosticKind.Warning"/>, as <c>dotnet build</c> does.
/// </remarks>
internal sealed class NuGetAssetsDiagnostics
{
    // Maps the path of a project file to the messages of its assets file, each with a value indicating whether it is an error.
    private readonly ConcurrentDictionary<string, IReadOnlyList<(string Message, bool IsError)>> _messagesByProject =
        new( StringComparer.OrdinalIgnoreCase );

    /// <summary>
    /// Reads the messages of the assets file of a project. The method can be called concurrently and several times for the same project,
    /// for instance once per target framework.
    /// </summary>
    /// <param name="projectPath">The full path of the project file.</param>
    /// <param name="assetsFilePath">The value of the <c>ProjectAssetsFile</c> property. A relative path is resolved against the
    /// directory of the project.</param>
    public void AddAssetsFile( string projectPath, string? assetsFilePath )
    {
        if ( string.IsNullOrEmpty( assetsFilePath ) )
        {
            return;
        }

        var fullPath = Path.GetFullPath( assetsFilePath, Path.GetDirectoryName( Path.GetFullPath( projectPath ) )! );

        this._messagesByProject.GetOrAdd( Path.GetFullPath( projectPath ), _ => ReadMessages( fullPath ) );
    }

    private static IReadOnlyList<(string Message, bool IsError)> ReadMessages( string path )
    {
        var messages = new List<(string Message, bool IsError)>();

        try
        {
            if ( !File.Exists( path ) )
            {
                return messages;
            }

            using var stream = File.OpenRead( path );
            using var document = JsonDocument.Parse( stream );

            if ( !document.RootElement.TryGetProperty( "logs", out var logs ) || logs.ValueKind != JsonValueKind.Array )
            {
                return messages;
            }

            foreach ( var log in logs.EnumerateArray() )
            {
                if ( log.TryGetProperty( "message", out var message ) && message.GetString() is { Length: > 0 } messageText )
                {
                    // A message without a level is treated as an error, so that we never downgrade it.
                    var isError = !log.TryGetProperty( "level", out var level )
                                  || string.Equals( level.GetString(), "Error", StringComparison.OrdinalIgnoreCase );

                    messages.Add( (messageText, isError) );
                }
            }
        }
        catch ( Exception e ) when ( e is IOException or UnauthorizedAccessException or JsonException )
        {
            // A missing or unreadable assets file only means that we cannot correct the severity.
        }

        return messages;
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
    // We consider only the assets files of the projects named in the diagnostic, and we downgrade the diagnostic only when every
    // matching message is a non-error, so that an error with the same text as a warning stays an error.
    private bool IsNonErrorMessage( string diagnosticMessage )
    {
        var matches = this._messagesByProject
            .Where( p => diagnosticMessage.Contains( $"'{p.Key}'", StringComparison.OrdinalIgnoreCase ) )
            .SelectMany( p => p.Value )
            .Where( m => diagnosticMessage.EndsWith( ": " + m.Message, StringComparison.Ordinal ) )
            .ToList();

        return matches.Count > 0 && matches.All( m => !m.IsError );
    }
}
