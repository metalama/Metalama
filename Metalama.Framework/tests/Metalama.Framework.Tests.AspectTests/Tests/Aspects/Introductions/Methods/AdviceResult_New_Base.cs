using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Linq;

#pragma warning disable CS0618 // IAdviceResult.AspectBuilder is obsolete

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Methods.AdviceResult_New_Base
{
    public class TestAspect : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            var result = builder.IntroduceMethod( nameof(Method), whenExists: OverrideStrategy.New );

            if (result.Outcome != AdviceOutcome.New)
            {
                throw new InvalidOperationException( $"Outcome was {result.Outcome} instead of New." );
            }

            if (result.AdviceKind != AdviceKind.IntroduceMethod)
            {
                throw new InvalidOperationException( $"AdviceKind was {result.AdviceKind} instead of IntroduceMethod." );
            }

            if (!builder.Advice.MutableCompilation.Comparers.Default.Equals(
                    result.Declaration.ForCompilation( builder.Advice.MutableCompilation ),
                    builder.Target.ForCompilation( builder.Advice.MutableCompilation ).Methods.OfName( "Method" ).Single() ))
            {
                throw new InvalidOperationException( $"Declaration was not correct." );
            }
        }

        [Template]
        public int Method()
        {
            Console.WriteLine( "Aspect code." );

            return meta.Proceed();
        }
    }

    public class BaseClass
    {
        public virtual int Method()
        {
            return 42;
        }
    }

    // <target>
    [TestAspect]
    public class TargetClass : BaseClass { }
}