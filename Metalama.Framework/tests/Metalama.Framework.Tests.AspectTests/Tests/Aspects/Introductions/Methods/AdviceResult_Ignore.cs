using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Linq;

#pragma warning disable CS0618 // IAdviceResult.AspectBuilder is obsolete

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Methods.AdviceResult_Ignore
{
    public class TestAspect : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            var result = builder.IntroduceMethod( nameof(Method), whenExists: OverrideStrategy.Ignore );

            if (result.Outcome != AdviceOutcome.Ignore)
            {
                throw new InvalidOperationException( $"Outcome was {result.Outcome} instead of Ignored." );
            }

            if (result.AdviceKind != AdviceKind.IntroduceMethod)
            {
                throw new InvalidOperationException( $"AdviceKind was {result.AdviceKind} instead of IntroduceMethod." );
            }

            if (result.Declaration != builder.Target.Methods.OfName( "Method" ).Single().ForCompilation( result.Declaration.Compilation ))
            {
                throw new InvalidOperationException( $"Declaration was not correct." );
            }
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
            Console.WriteLine( "Original code." );

            return 42;
        }
    }
}