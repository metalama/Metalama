using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Linq;

#pragma warning disable CS0618 // IAdviceResult.AspectBuilder is obsolete

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Events.AdviceResult_Override_Base
{
    public class TestAspect : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            var result = builder.IntroduceEvent( nameof(Event), whenExists: OverrideStrategy.Override );

            if (result.Outcome != AdviceOutcome.Override)
            {
                throw new InvalidOperationException( $"Outcome was {result.Outcome} instead of Override." );
            }

            if (result.AdviceKind != AdviceKind.IntroduceEvent)
            {
                throw new InvalidOperationException( $"AdviceKind was {result.AdviceKind} instead of IntroduceEvent." );
            }

            if (!builder.Advice.MutableCompilation.Comparers.Default.Equals(
                    result.Declaration.ForCompilation( builder.Advice.MutableCompilation ),
                    builder.Target.ForCompilation( builder.Advice.MutableCompilation ).Events.Single() ))
            {
                throw new InvalidOperationException( $"Declaration was not correct." );
            }
        }

        [Template]
        public event EventHandler Event
        {
            add
            {
                Console.WriteLine( "Aspect code." );
                meta.Proceed();
            }
            remove
            {
                Console.WriteLine( "Aspect code." );
                meta.Proceed();
            }
        }
    }

    public class BaseClass
    {
        public virtual event EventHandler Event
        {
            add { }
            remove { }
        }
    }

    // <target>
    [TestAspect]
    public class TargetClass : BaseClass { }
}