#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_4_8_0_OR_GREATER)
#endif

#if ROSLYN_4_8_0_OR_GREATER

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp12.RefReadonlyParameter_Override;

internal class TheAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        foreach (var parameter in meta.Target.Parameters)
        {
            Console.WriteLine( $"{parameter}: Kind={parameter.RefKind}, Value={parameter.Value}" );
        }

        return meta.Proceed();
    }
}

internal class C
{
    [TheAspect]
    private void M( in int i, ref readonly int j )
    {
        Console.WriteLine( i + j );
    }
}

#endif