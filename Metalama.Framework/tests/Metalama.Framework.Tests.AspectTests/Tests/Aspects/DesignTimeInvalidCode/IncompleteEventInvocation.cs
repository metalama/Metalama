#if TEST_OPTIONS
// @TestScenario(DesignTime)
# endif

using Metalama.Framework.Aspects;
using System.ComponentModel;

#pragma warning disable CS0067

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.DesignTimeInvalidCode.IncompleteEventInvocation;

class Aspect : OverrideMethodAspect
{
    [Introduce]
    public event PropertyChangedEventHandler? PropertyChanged1;

    [Introduce]
    public PropertyChangedEventHandler? PropertyChanged2;

    public override dynamic? OverrideMethod()
    {
#if TESTRUNNER
        this.PropertyChanged1(meta.This, );
        this.PropertyChanged2(meta.This, );
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