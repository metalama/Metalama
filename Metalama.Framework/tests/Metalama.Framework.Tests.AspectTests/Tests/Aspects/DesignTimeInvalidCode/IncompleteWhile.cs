#if TEST_OPTIONS
// @TestScenario(DesignTime)
# endif

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.DesignTimeInvalidCode.IncompleteWhile;

class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
#if TESTRUNNER
        while (
        {

        }
#endif

        return meta.Proceed();
    }
}

class Target
{
    [Aspect]
    void M()
    {
    }
}