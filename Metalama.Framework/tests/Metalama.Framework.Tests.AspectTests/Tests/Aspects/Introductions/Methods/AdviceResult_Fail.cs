using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

#pragma warning disable CS0618 // IAdviceResult.AspectBuilder is obsolete

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Methods.AdviceResult_Fail
{
    public class TestAspect : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            var result = builder.IntroduceMethod( nameof(Method), whenExists: OverrideStrategy.Fail );

            if (result.Outcome != AdviceOutcome.Error)
            {
                throw new InvalidOperationException( $"Outcome was {result.Outcome} instead of Error." );
            }

            if (result.AdviceKind != AdviceKind.IntroduceMethod)
            {
                throw new InvalidOperationException( $"AdviceKind was {result.AdviceKind} instead of IntroduceMethod." );
            }

            // TODO: #33060
            //if (result.Declaration != builder.Target.Events.Single())
            //{
            //    throw new InvalidOperationException($"Declaration was not correct.");
            //}
        }

        [Template]
        public int Method()
        {
            return 42;
        }
    }

    // <target>
    [TestAspect]
    public class TargetClass
    {
        public int Method()
        {
            return 42;
        }
    }
}