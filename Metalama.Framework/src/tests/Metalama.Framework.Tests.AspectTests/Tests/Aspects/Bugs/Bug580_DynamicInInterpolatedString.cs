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
        // Dynamic local variable in an interpolated string (the primary bug scenario).
        var result = meta.Proceed();

        Console.WriteLine( $"Method returned: {result}" );

        // Test with different blocks and a variable with the same name in a nested scope.
        // This ensures the fix is not based on tracking variable names (which would break
        // with conflicting names across scopes).
        if ( meta.Target.Parameters.Count > 0 )
        {
            var result2 = meta.Target.Parameters[0].Value;
            Console.WriteLine( $"  First param value: {result2}" );
        }

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
}
