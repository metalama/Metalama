using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.Subexpression;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        var x = CalledTemplate();
        Console.WriteLine( CalledTemplate() );

        for (CalledTemplate(); CalledTemplate(); CalledTemplate())
        {
            ;
        }

        return CalledTemplate();
    }

    [Template]
    private dynamic? CalledTemplate()
    {
        return meta.Proceed();
    }
}

internal class TargetCode
{
    // <target>
    [Aspect]
    private void Method() { }
}