using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplatingCodeValidation.Unsafe;

public unsafe class Aspect1 : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        void* ptr = null;

        return meta.Proceed();
    }
}

public class Aspect2 : OverrideMethodAspect
{
    public override unsafe dynamic? OverrideMethod()
    {
        void* ptr = null;

        return meta.Proceed();
    }

    [Introduce]
    public unsafe int* Property => null;
}