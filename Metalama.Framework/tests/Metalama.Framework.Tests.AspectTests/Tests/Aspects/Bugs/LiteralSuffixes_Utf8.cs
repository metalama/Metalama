using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.LiteralSuffixes_Utf8;

public class TestAspect : OverrideMethodAspect
{
    public override dynamic OverrideMethod()
    {
        var s1 = "littoral literal"u8;
        var s2 = s1;

        return meta.Proceed();
    }
}

public class TargetClass
{
    // <target>
    [TestAspect]
    public void Method() { }
}