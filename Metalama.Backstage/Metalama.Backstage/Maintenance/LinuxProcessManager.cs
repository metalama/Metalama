// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Metalama.Backstage.Maintenance;

internal sealed class LinuxProcessManager : ProcessManagerBase
{
    public LinuxProcessManager( IServiceProvider serviceProvider ) : base( serviceProvider ) { }

    protected override IEnumerable<KillableProcess> GetProcesses( ImmutableArray<KillableProcessSpec> processNames ) => this.GetDotNetProcesses( processNames );
}