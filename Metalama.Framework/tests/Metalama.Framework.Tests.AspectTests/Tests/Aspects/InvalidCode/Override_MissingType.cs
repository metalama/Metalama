using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.InvalidCode.Override_MissingType;

/*
 * Tests that call of a method of a missing type is handled by overrides.
 */

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( "Aspect" );

        return meta.Proceed();
    }
}

// <target>
internal class TargetCode
{
    [Aspect]
    private void Method( int a )
    {
#if TESTRUNNER
        Foo.Bar();
#endif
    }
}