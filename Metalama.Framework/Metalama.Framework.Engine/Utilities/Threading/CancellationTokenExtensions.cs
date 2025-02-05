// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Diagnostics;
using System.Threading;

namespace Metalama.Framework.Engine.Utilities.Threading;

public static class CancellationTokenExtensions
{
    [DebuggerStepThrough]
    public static TestableCancellationToken ToTestable( this CancellationToken cancellationToken ) => new( cancellationToken );
}