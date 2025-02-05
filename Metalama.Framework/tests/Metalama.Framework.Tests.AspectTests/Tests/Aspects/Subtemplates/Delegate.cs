using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.Delegate;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        var template = CalledTemplate;

        template();

        return default;
    }

    [Template]
    private void CalledTemplate()
    {
        Console.WriteLine( "called template" );
    }
}

// <target>
internal class TargetCode
{
    [Aspect]
    private void Method() { }
}