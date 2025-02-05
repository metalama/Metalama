using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.InvalidCode.DuplicateDeclarationWithAspect;

/*
 * Tests that when there are duplicate declarations, the error is produced without crashing.
 */

public class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        return meta.Proceed();
    }
}

// <target>
internal class TargetCode
{
    [Aspect]
    public int Foo()
    {
        return 42;
    }

#if TESTRUNNER
    [Aspect]
    public int Foo()
    {
        return 42;
    }
#endif
}