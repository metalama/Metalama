// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Infrastructure;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Metalama.Backstage.Testing;

public class TestProcessExecutor : IProcessExecutor
{
    public List<ProcessStartInfo> StartedProcesses { get; } = [];

    public IProcess Start( ProcessStartInfo startInfo )
    {
        this.StartedProcesses.Add( startInfo );

        return new TestProcess();
    }

    private class TestProcess : IProcess
    {
        public void Dispose() { }

        public int ExitCode => 0;

        event Action? IProcess.Exited { add { } remove { } }

        public bool HasExited => false;

        public void WaitForExit() { }
    }
}