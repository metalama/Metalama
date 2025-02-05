using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplateTypeParameter.DifferentReturnType;

public class Override : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        string LocalFunction()
        {
            // Invalid code generated here.
            meta.Proceed();

            return "something";
        }

        var s = LocalFunction();

        return s.Length;
    }
}

// <target>
internal class C
{
    [Override]
    private int Method_ExpressionBody() => 42;

    [Override]
    private int Method_BlockBody()
    {
        return 42;
    }
}