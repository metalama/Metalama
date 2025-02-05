using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug33532;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        return meta.Proceed();

        throw new NotImplementedException();
    }
}

// <target>
internal class Target
{
    [Aspect]
    private static void UnreachableAfterReturn()
    {
        return;

        throw new Exception();
    }

    [Aspect]
    private static void ReachableAfterReturn( int i )
    {
        if (i == 0)
        {
            goto label;
        }

        return;

    label:
        Console.WriteLine( "Test" );
    }
}