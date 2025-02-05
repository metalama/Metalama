#if TEST_OPTIONS
// @TestScenario(DesignTime)
#endif

#pragma warning disable CS0067

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Introductions.Events.DesignTime
{
    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder ) { }

        [Introduce]
        public event EventHandler? EventField;

        [Introduce]
        public event EventHandler? Event
        {
            add
            {
                Console.WriteLine( "Original add accessor." );
            }

            remove
            {
                Console.WriteLine( "Original add accessor." );
            }
        }
    }

    // <target>
    [Introduction]
    internal partial class TargetClass { }
}