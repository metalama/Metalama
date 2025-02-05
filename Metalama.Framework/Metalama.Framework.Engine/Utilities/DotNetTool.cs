// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Backstage.Infrastructure;
using Metalama.Framework.Engine.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Metalama.Framework.Engine.Utilities;

[PublicAPI]
public sealed class DotNetTool
{
    private readonly IPlatformInfo _platformInfo;

    public DotNetTool( GlobalServiceProvider serviceProvider )
    {
        this._platformInfo = serviceProvider.GetRequiredBackstageService<IPlatformInfo>();
    }

    public void Execute( string arguments, string? workingDirectory = null, int timeout = 30_000, Func<KeyValuePair<string, string?>, bool>? environmentVariableFilter = null )
    {
        var startInfo = new ProcessStartInfo( this._platformInfo.DotNetExePath, arguments )
        {
            // We cannot call dotnet.exe with a \\?\-prefixed path because MSBuild would fail.
            WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        
        // We must avoid passing the following environment variables to the child process, otherwise there can be a mismatch
        // between SDK versions and the build will fail.
        startInfo.Environment.Remove( "DOTNET_ROOT_X64" );
        startInfo.Environment.Remove( "MSBUILD_EXE_PATH" );
        startInfo.Environment.Remove( "MSBuildSDKsPath" );

        if ( environmentVariableFilter != null )
        {
            // If we have a filter delegate, update the startInfo accordingly.
            foreach ( var envVar in startInfo.Environment.ToArray() )
            {
                if ( !environmentVariableFilter( envVar ) )
                {
                    startInfo.Environment.Remove( envVar.Key );
                }
            }
        }

        var process = new Process() { StartInfo = startInfo };

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
            // The process did not complete within the given time.

            try
            {
                process.Kill();
            }
            catch
            {
                // ignored
            }

            throw new AssertionFailedException( $"The process '{startInfo.FileName} {startInfo.Arguments}' did not complete in {timeout / 1000f} s." );
        }

        if ( process.ExitCode != 0 )
        {
            throw new InvalidOperationException(
                $"Error calling `\"{this._platformInfo.DotNetExePath}\" {arguments}` in `{startInfo.WorkingDirectory}` returned {process.ExitCode}. Process output:"
                + Environment.NewLine + Environment.NewLine + string.Join( Environment.NewLine, lines ) );
        }
    }
}