// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug580_DynamicParameterInInterpolatedString;

internal class LogAttribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        // Using parameter values (dynamic) directly in an interpolated string.
        // This should NOT produce CS9230 in the generated code.
        foreach (var parameter in meta.Target.Parameters)
        {
            if (parameter.RefKind != RefKind.Out)
            {
                Console.WriteLine( $"  {parameter.Name} = {parameter.Value}" );
            }
        }

        return meta.Proceed();
    }
}

// <target>
internal class Target
{
    [Log]
    public int Add( int a, int b )
    {
        return a + b;
    }

    [Log]
    public dynamic Process( dynamic input )
    {
        return input;
    }

    private static void TestMain()
    {
        var target = new Target();
        target.Add( 1, 2 );
        target.Process( "hello" );
    }
}
