// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Diagnostics;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace Metalama.Backstage.Utilities;

internal class ParentProcessSearchMac : ParentProcessSearchBase<int>
{
    public ParentProcessSearchMac( ILogger logger ) : base( logger ) { }

    protected override bool IsNull( int handle ) => handle == 0;

    protected override int GetCurrentProcessHandle() => Process.GetCurrentProcess().Id;

    protected override (string? ImageName, int CurrentProcessId, int ParentProcessHandle) GetProcessInfo( int processHandle )
    {
        // There's no handle on Mac.
        var processId = processHandle;
        
        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "ps",
                Arguments = $"-o ppid= -o command= {processId}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using ( var cmdProcess = Process.Start( processStartInfo ) )
            {
                if ( cmdProcess == null )
                {
                    throw new InvalidOperationException( "Failed to start 'ps' command." );
                }
                
                cmdProcess.WaitForExit();
                var output = cmdProcess.StandardOutput.ReadToEnd().Trim();

                this.Logger.Trace?.Log( $"ps {processId} output: {output}" );

                var pidAndCommand = output.Split( new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries );

                // The remaining fields are command arguments.
                if ( pidAndCommand.Length < 2 )
                {
                    throw new InvalidOperationException( $"Unexpected output from 'ps' command: '{output}'." );
                }

                var parentProcessId = int.Parse( pidAndCommand[0], CultureInfo.InvariantCulture );

                // Examples:
                // -bash
                // /init
                // /usr/bin/dotnet
                var imageName = pidAndCommand[1]
                    .Split( new[] { '/' }, StringSplitOptions.RemoveEmptyEntries )
                    .Last()
                    .Trim();

                return (imageName, processId, parentProcessId);
            }
        }
        catch ( Exception ex )
        {
            Console.WriteLine( "Error reading parent process on macOS: " + ex.Message );

            throw;
        }
    }

    protected override void CloseProcessHandle( int handle ) { }
}