#if TEST_OPTIONS
// @TestScenario(DesignTime)
#endif

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.DesignTime.RecordStruct
{
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
    [Introduction]
    internal partial record struct TargetStructRecord( int x );
}