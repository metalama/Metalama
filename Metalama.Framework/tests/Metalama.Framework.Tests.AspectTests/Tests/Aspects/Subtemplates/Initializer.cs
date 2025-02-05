using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.Initializer;

internal class Aspect : TypeAspect
{
    [Introduce]
    private int i = Compute();

    [Template]
    private static int Compute()
    {
        Console.WriteLine( "called template" );

        return 42;
    }
}

// <target>
[Aspect]
internal class TargetCode { }