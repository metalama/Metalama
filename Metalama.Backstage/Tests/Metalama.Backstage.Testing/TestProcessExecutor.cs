// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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