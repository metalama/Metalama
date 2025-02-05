#if TEST_OPTIONS
// @TestScenario(DesignTime)
#endif

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.DesignTime.ClassInStruct;

public class IntroductionAttribute : TypeAspect
{
    [Introduce]
    public void M()
    {
        Console.WriteLine( "This is introduced method." );
        var nic = meta.Proceed();
    }
}

// <target>
internal partial struct TargetStruct
{
    [Introduction]
    private partial class Nested { }
}