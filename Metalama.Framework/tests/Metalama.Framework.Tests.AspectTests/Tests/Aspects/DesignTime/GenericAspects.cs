#if TEST_OPTIONS
// @TestScenario(DesignTime)
// @RequiredConstant(ROSLYN_4_4_0_OR_GREATER)
#endif

#if ROSLYN_4_4_0_OR_GREATER

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.DesignTime.GenericAspects;

// Tests that generic aspects do not cause an exception. The output does not include generic aspects because they are not yet supported.

public class TestAspect : TypeAspect
{
    [Introduce]
    public void M1()
    {
    }
}

public class TestAspect<T> : TypeAspect
{
    [Introduce]
    public T M2()
    {
        return default;
    }
}

public class TestAspect<T, U> : TypeAspect
{
    [Introduce]
    public T M3(U v)
    {
        return default;
    }
}

// <target>
[TestAspect]
[TestAspect<int>]
[TestAspect<int, int>]
internal partial class TargetClass
{
}

#endif