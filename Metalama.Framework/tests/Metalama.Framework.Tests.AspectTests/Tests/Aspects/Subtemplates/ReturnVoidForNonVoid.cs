#if TEST_OPTIONS
// @AcceptInvalidInput
#endif

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.ReturnVoidForNonVoid;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        CalledTemplate();

        return default;
    }

    [Template]
    private void CalledTemplate()
    {
        meta.Return();
    }
}

internal class TargetCode
{
    // <target>
    [Aspect]
    private int Method()
    {
        return 42;
    }
}