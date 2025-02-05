// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Diagnostics;

namespace Metalama.Framework.Engine.Utilities.Caching;

internal static class SharedStopwatch
{
    public static Stopwatch Instance { get; } = Stopwatch.StartNew();
}