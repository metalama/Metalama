// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Diagnostics;
using System.Collections.Generic;

namespace Metalama.Backstage.Utilities;

internal abstract class ParentProcessSearchBase
{
    public abstract IReadOnlyList<ProcessInfo> GetParentProcesses( ISet<string>? pivots = null );
    
    protected ILogger Logger { get; }

    protected ParentProcessSearchBase( ILogger logger )
    {
        this.Logger = logger;
    }
}