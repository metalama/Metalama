// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        var variablesToRemove = new[]
        {
            "DOTNET_ROOT_X64", "MSBUILD_EXE_PATH", "MSBuildSDKsPath", "MSBuildExtensionsPath", "Configuration"
        };

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

        // MSBuild node reuse keeps worker processes alive for about fifteen minutes so that a later build can reuse
        // them, which the short-lived builds started through this class have nothing to gain from. It is a re-entrancy
        // hazard as well, because the reference-assembly build runs inside the compiler, hence inside a task of a
        // build whose own nodes are occupied waiting for it. The command line of that build disables node reuse too;
        // this variable covers any further MSBuild that the child may start on its own. It is set after the filter,
        // because it is a requirement of the child process and not a variable inherited from this one. See issue
        // #1740.
        startInfo.Environment["MSBUILDDISABLENODEREUSE"] = "1";

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

            throw ProcessFailedException.CreateTimeout( this._msBuildExePath, arguments, startInfo.WorkingDirectory, timeout );
        }

        if ( process.ExitCode != 0 )
        {
            throw ProcessFailedException.CreateNonZeroExitCode(
                this._msBuildExePath,
                arguments,
                startInfo.WorkingDirectory,
                process.ExitCode,
                timeout,
                lines.ToImmutableArray() );
        }
    }
}