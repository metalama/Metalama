using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Linq;

#pragma warning disable CS0618 // IAdviceResult.AspectBuilder is obsolete

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Properties.AdviceResult_Ignore
{
    public class TestAspect : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            var result = builder.IntroduceProperty( nameof(Property), whenExists: OverrideStrategy.Ignore );

            if (result.Outcome != AdviceOutcome.Ignore)
            {
                throw new InvalidOperationException( $"Outcome was {result.Outcome} instead of Ignored." );
            }

            if (result.AdviceKind != AdviceKind.IntroduceProperty)
            {
                throw new InvalidOperationException( $"AdviceKind was {result.AdviceKind} instead of IntroduceProperty." );
            }

            if (result.Declaration != builder.Target.Properties.Single().ForCompilation( result.Declaration.Compilation ))
            {
                throw new InvalidOperationException( $"Declaration was not correct." );
            }
        }

        [Template]
        public int Property
        {
            get => 42;
            set { }
        }
    }

    // <target>
    [TestAspect]
    public class TargetClass
    {
        public int Property
        {
            get
            {
                Console.WriteLine( "Original code." );

                return 42;
            }
            set
            {
                Console.WriteLine( "Original code." );
            }
        }
    }
}