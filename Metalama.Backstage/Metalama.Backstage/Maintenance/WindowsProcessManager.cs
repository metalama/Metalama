// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Metalama.Backstage.Maintenance;

internal class WindowsProcessManager : ProcessManagerBase
{
    public WindowsProcessManager( IServiceProvider serviceProvider ) : base( serviceProvider ) { }

    protected override IEnumerable<KillableProcess> GetProcesses( ImmutableArray<KillableProcessSpec> processNames )
        => this.GetDotNetProcesses( processNames ).Concat( this.GetStandaloneCompilerProcesses( processNames ) );

#pragma warning disable CA1307
    private IEnumerable<KillableProcess> GetStandaloneCompilerProcesses( ImmutableArray<KillableProcessSpec> processNames )
    {
        foreach ( var processSpec in processNames.Where( p => p.IsStandaloneProcess ) )
        {
            foreach ( var process in Process.GetProcessesByName( processSpec.Name.ToLowerInvariant() ) )
            {
                if ( !this.TryGetModulePaths( process, out var modules ) )
                {
                    continue;
                }

                if ( this.ReferencesMetalama( process, modules ) == false )
                {
                    this.Logger.Trace?.Log( $"Do not kill '{process.ProcessName}' ({process.Id}) because it does not contain Metalama." );
                }

                yield return new KillableProcess( process, this.Logger, null, processSpec );
            }
        }
    }
}