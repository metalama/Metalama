using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.Property;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        return P;
    }

    [Template]
    private int P
    {
        get
        {
            Console.WriteLine( "property subtemplate" );

            return 42;
        }
    }
}

// <target>
internal class TargetCode
{
    [Aspect]
    private int Method()
    {
        return 0;
    }
}