using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.InvalidCode.UnknownAccessor;

/*
 * Tests that invalid accessor declarations do not crash.
 */

internal class Aspect : OverrideFieldOrPropertyAspect
{
    public override dynamic? OverrideProperty
    {
        get
        {
            return meta.Proceed();
        }

        set
        {
            meta.Proceed();
        }
    }
}

// <target>
internal class TargetCode
{
#if TESTRUNNER
    [Aspect]
    public int Foo 
    { 
        getx;
    }

    [Aspect]
    public int Bar
    { 
        ;
    }
#endif
}