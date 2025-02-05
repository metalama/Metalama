using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Linq;

#pragma warning disable CS0618 // IAdviceResult.AspectBuilder is obsolete

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Fields.AdviceResult_New_Base
{
    public class TestAspect : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            var result = builder.IntroduceField( nameof(Field), whenExists: OverrideStrategy.New );

            if (result.Outcome != AdviceOutcome.New)
            {
                throw new InvalidOperationException( $"Outcome was {result.Outcome} instead of New." );
            }

            if (result.AdviceKind != AdviceKind.IntroduceField)
            {
                throw new InvalidOperationException( $"AdviceKind was {result.AdviceKind} instead of IntroduceField." );
            }

            if (!builder.Target.Compilation.Comparers.Default.Equals(
                    result.Declaration.ForCompilation( builder.Advice.MutableCompilation ),
                    builder.Target.ForCompilation( builder.Advice.MutableCompilation ).Fields.Single() ))
            {
                throw new InvalidOperationException( $"Declaration was not correct." );
            }
        }

        [Template]
        public int Field;
    }

    public class BaseClass
    {
        public int Field;
    }

    // <target>
    [TestAspect]
    public class TargetClass : BaseClass { }
}