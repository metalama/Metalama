namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplatingCodeValidation.UseProceedInTemplate;

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

internal class RetryAttribute : OverrideMethodAspect
{
    // Template that overrides the methods to which the aspect is applied.
    public override dynamic? OverrideMethod()
    {
        return meta.Proceed();
    }

    [Template]
    public void Method()
    {
        meta.Proceed();
    }
}