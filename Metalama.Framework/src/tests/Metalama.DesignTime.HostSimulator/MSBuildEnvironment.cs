// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.Build.Locator;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Metalama.DesignTime.HostSimulator;

/// <summary>
/// Registers the .NET SDK whose MSBuild is used to open the simulated solution.
/// </summary>
/// <remarks>
/// This mirrors <c>Metalama.Framework.Workspaces.MSBuildInitializer</c>: the SDK is chosen from
/// <c>dotnet --list-sdks</c> rather than from <see cref="MSBuildLocator.QueryVisualStudioInstances()"/>, because the
/// latter does not find SDKs installed by <c>dotnet-install.ps1</c>. Registration must happen before any MSBuild or
/// workspace type is touched, so nothing in this class may reference <c>Microsoft.CodeAnalysis.Workspaces.MSBuild</c>.
/// </remarks>
internal static class MSBuildEnvironment
{
    private static readonly Regex _sdkListPattern =
        new( @"^(?<version>[0-9]+(?:\.[0-9]+)*(?:-[A-Za-z0-9\.]+)?)\s+\[(?<directory>[^\]]+)\]$", RegexOptions.Compiled );

    /// <summary>
    /// Registers the highest .NET SDK that the current runtime can host.
    /// </summary>
    public static void Register()
    {
        if ( MSBuildLocator.IsRegistered )
        {
            return;
        }

        if ( !MSBuildLocator.CanRegister )
        {
            throw new InvalidOperationException( "MSBuild assemblies are already loaded, so MSBuildLocator cannot be registered." );
        }

        var sdks = ListSdks();

        var selected = sdks
            .Where( sdk => sdk.Version.Major <= Environment.Version.Major )
            .OrderByDescending( sdk => sdk.Version )
            .FirstOrDefault( sdk => HasMatchingProcessorArchitecture( sdk.Directory ) );

        if ( selected.Directory == null )
        {
            throw new InvalidOperationException(
                $"Cannot find a .NET SDK compatible with the current runtime (.NET {Environment.Version}, {RuntimeInformation.RuntimeIdentifier}). "
                + $"Found: {string.Join( ", ", sdks.Select( sdk => sdk.RawVersion ) )}." );
        }

        // VisualStudioInstance has no public constructor, and MSBuildLocator offers no way to register a directory.
        var constructor =
            typeof(VisualStudioInstance).GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                [typeof(string), typeof(string), typeof(Version), typeof(DiscoveryType)] )
            ?? throw new InvalidOperationException( $"Cannot find the internal constructor of {nameof(VisualStudioInstance)}." );

        var instance = (VisualStudioInstance) constructor.Invoke(
            [$".NET SDK {selected.RawVersion}", selected.Directory, selected.Version, DiscoveryType.DotNetSdk] );

        MSBuildLocator.RegisterInstance( instance );
    }

    private static IReadOnlyList<(Version Version, string RawVersion, string Directory)> ListSdks()
    {
        var startInfo = new ProcessStartInfo( "dotnet", "--list-sdks" )
        {
            RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true
        };

        using var process = Process.Start( startInfo )
                            ?? throw new InvalidOperationException( "Cannot start 'dotnet --list-sdks'." );

        var output = new StringBuilder();
        var error = new StringBuilder();
        process.OutputDataReceived += ( _, e ) => output.AppendLine( e.Data );

        // Both redirected streams have to be drained. Reading only one of them lets the other fill its pipe buffer
        // and block the child forever, and it also loses the diagnostic that explains a non-zero exit code.
        process.ErrorDataReceived += ( _, e ) => error.AppendLine( e.Data );

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        if ( process.ExitCode != 0 )
        {
            throw new InvalidOperationException(
                $"'dotnet --list-sdks' failed with exit code {process.ExitCode}: {error.ToString().Trim()}" );
        }

        return output.ToString()
            .Split( '\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries )
            .Select( line => _sdkListPattern.Match( line ) )
            .Where( match => match.Success )
            .Select( match => (RawVersion: match.Groups["version"].Value, Root: match.Groups["directory"].Value) )
            .Select(
                x => (Parsed: Version.TryParse( x.RawVersion.Split( '-' )[0], out var version ), Version: version, x.RawVersion,
                      Directory: Path.Combine( x.Root, x.RawVersion )) )
            .Where( x => x.Parsed )
            .Select( x => (x.Version!, x.RawVersion, x.Directory) )
            .ToList();
    }

    /// <summary>
    /// Determines whether an SDK targets the same processor architecture as the current process, by reading the
    /// third line of its <c>.version</c> file.
    /// </summary>
    private static bool HasMatchingProcessorArchitecture( string directory )
    {
        var versionFile = Path.Combine( directory, ".version" );

        if ( !File.Exists( versionFile ) )
        {
            return false;
        }

        var lines = File.ReadAllLines( versionFile );

        return lines.Length >= 3
               && string.Equals( lines[2].Trim(), RuntimeInformation.RuntimeIdentifier, StringComparison.OrdinalIgnoreCase );
    }
}
