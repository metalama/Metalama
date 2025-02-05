// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Metalama.Backstage.Maintenance;

internal class LinuxProcessManager : ProcessManagerBase
{
    public LinuxProcessManager( IServiceProvider serviceProvider ) : base( serviceProvider ) { }

    protected override IEnumerable<KillableProcess> GetProcesses( ImmutableArray<KillableProcessSpec> processNames ) => this.GetDotNetProcesses( processNames );
}