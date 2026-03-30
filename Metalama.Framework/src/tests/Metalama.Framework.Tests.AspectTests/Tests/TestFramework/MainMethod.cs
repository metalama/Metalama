// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;

#if TEST_OPTIONS
// @MainMethod(Main)
#endif

namespace Metalama.Framework.Tests.AspectTests.Tests.TestFramework.MainMethod;

public class LogAttribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( $"Entering {meta.Target.Method.Name}" );

        try
        {
            return meta.Proceed();
        }
        finally
        {
            Console.WriteLine( $"Leaving {meta.Target.Method.Name}" );
        }
    }
}

internal class Program
{
    [Log]
    public static void Main()
    {
        Console.WriteLine( "Hello" );
    }
}
