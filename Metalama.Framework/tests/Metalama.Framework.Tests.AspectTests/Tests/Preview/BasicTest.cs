#if TEST_OPTIONS
// @TestScenario(Preview)
#endif

using System;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Preview.BasicTest;

internal class TheAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( "Oops" );

        return meta.Proceed();
    }
}

internal class C
{
    [TheAspect]
    public void M() { }
}