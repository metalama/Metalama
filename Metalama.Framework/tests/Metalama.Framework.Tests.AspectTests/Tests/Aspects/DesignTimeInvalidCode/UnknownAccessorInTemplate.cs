#if TEST_OPTIONS
// @TestScenario(DesignTime)
#endif

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.DesignTimeInvalidCode.UnknownAccessorInTemplate;

/*
 * Tests that invalid accessor declarations in a template do not crash.
 */

internal class Aspect : PropertyAspect
{
    [Template]
    public dynamic? Template
    {
        get
        {
            return meta.Proceed();
        }

#if TESTRUNNER
        setx
        {
            meta.Proceed();
        }
#endif
    }
}