using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Linq;

#pragma warning disable CS0618 // IAdviceResult.AspectBuilder is obsolete

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Events.AdviceResult_Ignore
{
    public class TestAspect : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            var result = builder.IntroduceEvent( nameof(Event), whenExists: OverrideStrategy.Ignore );

            if (result.Outcome != AdviceOutcome.Ignore)
            {
                throw new InvalidOperationException( $"Outcome was {result.Outcome} instead of Ignore." );
            }

            if (result.AdviceKind != AdviceKind.IntroduceEvent)
            {
                throw new InvalidOperationException( $"AdviceKind was {result.AdviceKind} instead of IntroduceEvent." );
            }

            if (result.Declaration != builder.Target.Events.Single().ForCompilation( result.Declaration.Compilation ))
            {
                throw new InvalidOperationException( $"Declaration was not correct." );
            }
        }

        [Template]
        public event EventHandler Event
        {
            add { }
            remove { }
        }
    }

    // <target>
    [TestAspect]
    public class TargetClass
    {
        public event EventHandler Event
        {
            add
            {
                Console.WriteLine( "Original code." );
            }
            remove
            {
                Console.WriteLine( "Original code." );
            }
        }
    }
}