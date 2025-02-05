// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using System;

namespace Metalama.Backstage.Infrastructure;

[PublicAPI]
public interface IProcess : IDisposable
{
    int ExitCode { get; }
    
    event Action Exited;

    bool HasExited { get; }

    void WaitForExit();
}