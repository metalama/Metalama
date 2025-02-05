#if TEST_OPTIONS
// @TestScenario(DesignTime)
#endif

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.DesignTimeNonPartial
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce]
        public void IntroducedMethod_Void()
        {
            Console.WriteLine( "This method should not be introduced in design time because the target class is not partial." );
            var nic = meta.Proceed();
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass { }
}