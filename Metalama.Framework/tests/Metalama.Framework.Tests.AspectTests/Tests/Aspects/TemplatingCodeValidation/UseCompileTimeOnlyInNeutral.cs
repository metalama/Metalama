using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplatingCodeValidation.UseMetaOutOfTemplate;

internal class RetryAttribute : OverrideMethodAspect
{
    // Template that overrides the methods to which the aspect is applied.
    public override dynamic? OverrideMethod()
    {
        return meta.Proceed();
    }

    public void Method()
    {
        IDeclaration? d = null;
        _ = d;
    }
}