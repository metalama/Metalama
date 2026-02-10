// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @WriteCompiledTemplate
#endif

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

        // Test with conflicting variable names across scopes.
        // The first declaration is in the 'else' block so both branches declare 'result2'
        // with the same name, verifying that scoping is handled correctly.
        if ( meta.Target.Parameters.Count > 0 )
        {
            var result2 = meta.Target.Parameters[0].Value;
            Console.WriteLine( $"  First param value: {result2}" );
        }
        else
        {
            var result2 = meta.Proceed();
            Console.WriteLine( $"  No params, result2: {result2}" );
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
