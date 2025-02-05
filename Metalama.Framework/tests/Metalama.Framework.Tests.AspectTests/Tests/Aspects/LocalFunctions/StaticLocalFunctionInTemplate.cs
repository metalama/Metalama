using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.LocalFunctions.StaticLocalFunctionInTemplate;

public class TestAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        static void Local() { }

        Local();

        return meta.Proceed();
    }
}

// <target>
internal class C
{
    [TestAspect]
    private void Foo() { }
}