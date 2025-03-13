// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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