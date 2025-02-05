using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.InvalidCode.DuplicateDeclaration;

/*
 * Tests that ambiguous declaration does not cause a crash in the linker. The output may not be correct.
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
    private int Method( int a )
    {
        return a;
    }

#if TESTRUNNER
    int Method(int a)
    {
        return a;
    }
#endif
}