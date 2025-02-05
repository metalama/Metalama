using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.InvalidCode.Override_MissingMethod;

/*
 * Tests that call of a missing method is handled by overrides.
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
        Bar();
#endif
    }
}