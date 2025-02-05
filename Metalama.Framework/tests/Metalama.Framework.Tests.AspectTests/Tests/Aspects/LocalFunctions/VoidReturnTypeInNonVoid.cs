using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplateTypeParameter.VoidReturnTypeInNonVoid;

public class Override : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        void LocalFunction()
        {
            meta.Proceed();
        }

        LocalFunction();

        return default;
    }
}

// <target>
internal class TargetClass
{
    [Override]
    private int Method()
    {
        return 42;
    }

    [Override]
    private int Method_ExpressionBody() => 42;
}