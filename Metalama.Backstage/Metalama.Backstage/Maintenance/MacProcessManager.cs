// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Metalama.Backstage.Maintenance;

internal class MacProcessManager : ProcessManagerBase
{
    public MacProcessManager( IServiceProvider serviceProvider ) : base( serviceProvider ) { }

    protected override bool TryGetModulePaths( Process process, [NotNullWhen( true )] out List<string>? modules )
    {
        modules = [];

        var listOpenFilesProcess = new Process()
        {
            StartInfo = new ProcessStartInfo() { FileName = "lsof", Arguments = $"-p {process.Id}", RedirectStandardOutput = true }
        };

        listOpenFilesProcess.Start();
        listOpenFilesProcess.WaitForExit();

#pragma warning disable CA1307
        while ( !listOpenFilesProcess.StandardOutput.EndOfStream )
        {
            var outputLine = listOpenFilesProcess.StandardOutput.ReadLine();

            if ( outputLine != null )
            {
                var module = outputLine.Split( ' ' ).LastOrDefault();

                if ( module != null )
                {
                    modules.Add( module );
                }
            }
        }

        return true;
    }

    protected override IEnumerable<KillableProcess> GetProcesses( ImmutableArray<KillableProcessSpec> processNames )
    {
        return this.GetDotNetProcesses( processNames );
    }
}