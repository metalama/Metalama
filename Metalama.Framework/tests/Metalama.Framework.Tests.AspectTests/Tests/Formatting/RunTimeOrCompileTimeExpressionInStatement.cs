using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Formatting.RunTimeOrCompileTimeExpressionInStatement;

public class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        if (true)
        {
        }

        if ((true))
        {
        }

#if TESTRUNNER
        if (()meta.Proceed())
        {
        }
#endif

        foreach (var x in new[] { 42 })
        {
        }

        do
        {

        } while (false);

        while (true)
        {
        }
    }
}