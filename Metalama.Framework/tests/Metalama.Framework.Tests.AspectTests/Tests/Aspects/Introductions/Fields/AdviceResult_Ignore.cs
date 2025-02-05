using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Linq;

#pragma warning disable CS0618 // IAdviceResult.AspectBuilder is obsolete

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Fields.AdviceResult_Ignore
{
    public class TestAspect : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            var result = builder.IntroduceField( nameof(Field), whenExists: OverrideStrategy.Ignore );

            if (result.Outcome != AdviceOutcome.Ignore)
            {
                throw new InvalidOperationException( $"Outcome was {result.Outcome} instead of Ignore." );
            }

            if (result.AdviceKind != AdviceKind.IntroduceField)
            {
                throw new InvalidOperationException( $"AdviceKind was {result.AdviceKind} instead of IntroduceField." );
            }

            if (result.Declaration != builder.Target.Fields.Single().ForCompilation( result.Declaration.Compilation ))
            {
                throw new InvalidOperationException( $"Declaration was not correct." );
            }
        }

        [Template]
        public int Field;
    }

    // <target>
    [TestAspect]
    public class TargetClass
    {
        public int Field;
    }
}