using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.DynamicReturn;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( "regular template" );
        CalledTemplate();

        throw new Exception();
    }

    [Template]
    private dynamic? CalledTemplate()
    {
        Console.WriteLine( "called template" );

        return meta.Proceed();
    }
}

// <target>
internal class TargetCode
{
    [Aspect]
    private void VoidMethod() { }

    [Aspect]
    private int IntMethod()
    {
        return 42;
    }
}