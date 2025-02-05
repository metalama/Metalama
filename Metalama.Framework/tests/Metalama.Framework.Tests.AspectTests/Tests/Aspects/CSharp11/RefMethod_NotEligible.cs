using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp11.RefMethod_NotEligible;

public class TheAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        return meta.Proceed();
    }
}

internal class C
{
    private int _x;

    [TheAspect]
    public ref int GetX() => ref _x;
}