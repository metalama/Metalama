using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

#pragma warning disable CS0618 // IAdviceResult.AspectBuilder is obsolete

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Events.AdviceResult_New_Existing
{
    public class TestAspect : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            var result = builder.IntroduceEvent( nameof(Event), whenExists: OverrideStrategy.New );

            if (result.Outcome != AdviceOutcome.Error)
            {
                throw new InvalidOperationException( $"Outcome was {result.Outcome} instead of Error." );
            }

            if (result.AdviceKind != AdviceKind.IntroduceEvent)
            {
                throw new InvalidOperationException( $"AdviceKind was {result.AdviceKind} instead of IntroduceEvent." );
            }

            // TODO: #33060
            //if (!builder.Target.Compilation.Comparers.Default.Equals(result.Declaration.ForCompilation(builder.Advice.MutableCompilation), builder.Target.Events.Single()))
            //{
            //    throw new InvalidOperationException($"Declaration was not correct.");
            //}
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