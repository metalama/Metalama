// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Metalama.Framework.Engine.Utilities;

/// <summary>
/// Executes MSBuild.exe from Visual Studio or Build Tools.
/// Used when building compile-time projects in environments where the .NET SDK is not available
/// (e.g., old-style .NET Framework projects built with msbuild.exe).
/// </summary>
[PublicAPI]
public sealed class MSBuildTool
{
    private readonly string _msBuildExePath;

    public MSBuildTool( string msBuildBinPath )
    {
        this._msBuildExePath = Path.Combine( msBuildBinPath, "MSBuild.exe" );

        if ( !File.Exists( this._msBuildExePath ) )
        {
            throw new InvalidOperationException(
                $"Cannot find MSBuild.exe at '{this._msBuildExePath}'. The MSBuildBinPath property value '{msBuildBinPath}' is invalid." );
        }
    }

    public void Execute(
        string arguments,
        string? workingDirectory = null,
        int timeout = 30_000,
        Func<KeyValuePair<string, string?>, bool>? environmentVariableFilter = null )
    {
        var startInfo = new ProcessStartInfo( this._msBuildExePath, arguments )
        {
            WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        // Remove environment variables that can interfere with MSBuild execution.
        // These variables may point to .NET SDK paths that conflict with Visual Studio's MSBuild.
        var variablesToRemove = new[] { "DOTNET_ROOT_X64", "MSBUILD_EXE_PATH", "MSBuildSDKsPath", "Configuration" };

        foreach ( var key in startInfo.Environment.Keys
                     .Where( k => variablesToRemove.Any( v => k.Equals( v, StringComparison.OrdinalIgnoreCase ) ) )
                     .ToArray() )
        {
            startInfo.Environment.Remove( key );
        }

        if ( environmentVariableFilter != null )
        {
            foreach ( var envVar in startInfo.Environment.ToArray() )
            {
                if ( !environmentVariableFilter( envVar ) )
                {
                    startInfo.Environment.Remove( envVar.Key );
                }
            }
        }

        // ReSharper disable once UsingStatementResourceInitialization
        using var process = new Process { StartInfo = startInfo };

        var lines = new List<string>();

        void OnProcessDataReceived( object sender, DataReceivedEventArgs e )
        {
            lines.Add( e.Data ?? "" );
        }

        process.OutputDataReceived += OnProcessDataReceived;
        process.ErrorDataReceived += OnProcessDataReceived;

        process.Start();
        process.BeginErrorReadLine();
        process.BeginOutputReadLine();

        if ( !process.WaitForExit( timeout ) )
        {
            try
            {
                process.Kill();
            }
            catch
            {
                // ignored
            }

            throw new AssertionFailedException( $"The process '{this._msBuildExePath} {arguments}' did not complete in {timeout / 1000f} s." );
        }

        if ( process.ExitCode != 0 )
        {
            throw new InvalidOperationException(
                $"Error calling `\"{this._msBuildExePath}\" {arguments}` in `{startInfo.WorkingDirectory}` returned {process.ExitCode}. Process output:"
                + Environment.NewLine + Environment.NewLine + string.Join( Environment.NewLine, lines ) );
        }
    }
}