// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @WriteOutputHtml
#endif

using Metalama.Framework.Aspects;
using System;

namespace HtmlWriterAspectTest;

public class LogAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( $"Entering {meta.Target.Method.Name}" );
        var result = meta.Proceed();
        Console.WriteLine( $"Leaving {meta.Target.Method.Name}" );

        return result;
    }
}

// <target>
internal class Target
{
    [LogAspect]
    public static void DoWork()
    {
        Console.WriteLine( "Working..." );
    }
}
