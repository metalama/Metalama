using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.TemplateProvider_SideEffects;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( "regular template" );

        new Templates().CalledTemplate();
        new Templates().CalledTemplate();

        return meta.Proceed();
    }
}

internal class Templates : ITemplateProvider
{
    private static int i;

    public Templates()
    {
        i++;
    }

    [Template]
    public void CalledTemplate()
    {
        Console.WriteLine( $"called template i={i}" );
    }
}

internal class TargetCode
{
    // <target>
    [Aspect]
    private void Method() { }
}