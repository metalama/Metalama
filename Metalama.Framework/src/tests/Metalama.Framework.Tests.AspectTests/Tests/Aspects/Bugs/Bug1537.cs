// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Diagnostics;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug1537;

internal class StopwatchAttribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        var stopWatch = Stopwatch.StartNew();

        dynamic? returnValue = meta.Proceed();

        stopWatch.Stop();

        Trace.WriteLine( string.Format( format: "Method {0} executed in {1} ms.",
                                        arg0: meta.Target.Method.Name,
                                        arg1: stopWatch.ElapsedMilliseconds ) );

        return returnValue;
    }
}

// <target>
internal class TargetCode
{
    [Stopwatch]
    private int M( int a, int b )
    {
        return a + b;
    }
}
