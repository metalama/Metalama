using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.Empty_DynamicCall;

internal sealed class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        meta.InvokeTemplate( nameof(OverrideAsyncMethod) );

        return meta.Proceed();
    }
}

// <target>
internal class TargetCode
{
    [Aspect]
    private void Method() { }
}