// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Metalama.Backstage.Maintenance;

internal sealed class LinuxProcessManager : ProcessManagerBase
{
    public LinuxProcessManager( IServiceProvider serviceProvider ) : base( serviceProvider ) { }

    protected override IEnumerable<KillableProcess> GetProcesses( ImmutableArray<KillableProcessSpec> processNames ) => this.GetDotNetProcesses( processNames );

    protected override bool TryGetModulePaths( Process process, [NotNullWhen( true )] out List<string>? modules )
    {
        // On Linux, process.Modules only returns native modules (libcoreclr.so, dotnet, etc.)
        // but not managed assemblies loaded via 'dotnet exec'. Read /proc/<pid>/cmdline instead,
        // which contains the full command line including paths to DLLs.
        modules = null;

        try
        {
            var cmdlinePath = $"/proc/{process.Id}/cmdline";

            if ( !File.Exists( cmdlinePath ) )
            {
                return false;
            }

            var cmdline = File.ReadAllText( cmdlinePath );

            // /proc/<pid>/cmdline uses null bytes as argument separators.
            var args = cmdline.Split( new[] { '\0' }, StringSplitOptions.RemoveEmptyEntries );

            modules = new List<string>();

            foreach ( var arg in args )
            {
                // Include arguments that look like file paths to DLLs or executables.
                if ( arg.EndsWith( ".dll", StringComparison.OrdinalIgnoreCase ) || arg.EndsWith( ".exe", StringComparison.OrdinalIgnoreCase ) )
                {
                    modules.Add( arg );
                }
            }

            if ( modules.Count > 0 )
            {
                return true;
            }

            this.Logger.Trace?.Log( $"No DLL/EXE arguments found in /proc/{process.Id}/cmdline." );

            return false;
        }
        catch ( Exception e )
        {
            if ( !process.HasExited )
            {
                this.Logger.Warning?.Log( $"Cannot read /proc/{process.Id}/cmdline: {e.Message}." );
            }

            return false;
        }
    }
}