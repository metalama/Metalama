#if TEST_OPTIONS
// @TestScenario(DesignTime)
# endif

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.DesignTimeInvalidCode.InvalidLoopCondition;

class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
#if TESTRUNNER
        while (expression)
        {
        }

        do
        {
        } while (expression);

        foreach (var x in expression)
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