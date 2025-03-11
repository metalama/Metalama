// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Metalama.Backstage.Maintenance;

internal sealed class WindowsProcessManager : ProcessManagerBase
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