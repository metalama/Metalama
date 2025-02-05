using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

#pragma warning disable CS0618 // IAdviceResult.AspectBuilder is obsolete

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Properties.AdviceResult_Fail
{
    public class TestAspect : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            var result = builder.IntroduceProperty( nameof(Property), whenExists: OverrideStrategy.Fail );

            if (result.Outcome != AdviceOutcome.Error)
            {
                throw new InvalidOperationException( $"Outcome was {result.Outcome} instead of Ignored." );
            }

            if (result.AdviceKind != AdviceKind.IntroduceProperty)
            {
                throw new InvalidOperationException( $"AdviceKind was {result.AdviceKind} instead of IntroduceProperty." );
            }

            // TODO: #33060
            //if (result.Declaration != builder.Target.Events.Single())
            //{
            //    throw new InvalidOperationException($"Declaration was not correct.");
            //}
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
            get => 42;
            set { }
        }
    }
}