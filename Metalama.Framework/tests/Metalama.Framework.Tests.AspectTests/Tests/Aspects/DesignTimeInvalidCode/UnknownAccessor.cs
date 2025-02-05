#if TEST_OPTIONS
// @TestScenario(DesignTime)
#endif

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.DesignTimeInvalidCode.UnknownAccessor;

/*
 * Tests that invalid accessor declaration do not crash.
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
#endif
}