#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_4_8_0_OR_GREATER)
#endif

#if ROSLYN_4_8_0_OR_GREATER

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.NestedCompileTimeElseIf;

internal class Aspect : TypeAspect
{
    public int I { get; set; }

    [Introduce]
    private void M()
    {
        Console.WriteLine( I );

        if (I > 0)
        {
            if (I > 10)
            {
                Console.WriteLine( "I > 10" );
            }
            else if (I > 100)
            {
                Console.WriteLine( "I > 100" );
            }
        }
        else
        {
            Console.WriteLine( "I <= 0" );
        }
    }
}

// <target>
[Aspect( I = -1 )]
internal class TargetM1;

// <target>
[Aspect( I = 1 )]
internal class Target1;

#endif