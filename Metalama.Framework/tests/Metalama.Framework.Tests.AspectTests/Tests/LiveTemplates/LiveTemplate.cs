#if TEST_OPTIONS
// @TestScenario(LiveTemplate)
#endif

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Testing.AspectTesting;

namespace Metalama.Framework.IntegrationTests.LiveTemplates.LiveTemplate
{
    public class TestAspect : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            Console.WriteLine( "This is the overriding method." );

            return meta.Proceed();
        }
    }

    // <target>
    internal class TargetClass
    {
        [TestLiveTemplate(typeof(TestAspect))]
        public void TargetMethod()
        {
            Console.WriteLine( "This is the original method." );
        }
    }
}