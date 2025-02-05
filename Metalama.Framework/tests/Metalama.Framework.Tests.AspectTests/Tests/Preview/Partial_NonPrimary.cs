#if TEST_OPTIONS
// @TestScenario(Preview)
// @TargetSyntaxTreeSuffix(Other)
#endif

using System;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Preview.Partial_NonPrimary;

internal class TestAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine("Transformed");

        return meta.Proceed();
    }
}

internal partial class TargetClass
{
    [TestAspect]
    public void Foo() { }
}