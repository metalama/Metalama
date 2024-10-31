#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_4_12_0_OR_GREATER)
#endif

#if ROSLYN_4_12_0_OR_GREATER

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp13.MethodGroupNaturalType;

public class TheAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        var m = new C().M;
        m();

        return meta.Proceed();
    }
}

// <target>
class Target
{
    [TheAspect]
    void M()
    {
        var m = new C().M;
        m();
    }
}

class C
{
    public void M() { }
}

static class E
{
    public static void M(this C c, object o) { }
}

#endif