#if TEST_OPTIONS
// @TestScenario(Preview)
// @TargetSyntaxTreeSuffix(YetAnother)
#endif

using System;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Preview.Partial_MultipleTypes;

internal class TestAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( "Transformed" );

        return meta.Proceed();
    }
}

internal partial class TargetClass
{
    partial class NestedClass1
    {
        [TestAspect]
        public void Foo() { }
    }
}