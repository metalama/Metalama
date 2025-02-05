using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp11.RefProperty_NotEligible;

public class TheAspect : OverrideFieldOrPropertyAspect
{
    public override dynamic? OverrideProperty
    {
        get => meta.Proceed();
        set => meta.Proceed();
    }
}

internal class C
{
    private int _x;

    [TheAspect]
    public ref int X => ref _x;
}