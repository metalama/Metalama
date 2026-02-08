// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug580_DynamicInInterpolatedString;

internal class LogAttribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        // Using dynamic value (meta.Proceed() result, which is dynamic?) directly in an interpolated string.
        // This should NOT produce CS9230 in the generated code.
        var result = meta.Proceed();

        Console.WriteLine( $"Method returned: {result}" );

        return result;
    }
}

// <target>
internal class Target
{
    [Log]
    public dynamic DoSomething( dynamic input )
    {
        return input;
    }

    private static void TestMain()
    {
        var target = new Target();
        target.DoSomething( "hello" );
    }
}
