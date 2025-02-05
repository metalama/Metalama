using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

#pragma warning disable CS0618 // IAdviceResult.AspectBuilder is obsolete

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Methods.AdviceResult_New_Existing
{
    public class TestAspect : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            var result = builder.IntroduceMethod( nameof(Method), whenExists: OverrideStrategy.New );

            if (result.Outcome != AdviceOutcome.Error)
            {
                throw new InvalidOperationException( $"Outcome was {result.Outcome} instead of Error." );
            }

            if (result.AdviceKind != AdviceKind.IntroduceMethod)
            {
                throw new InvalidOperationException( $"AdviceKind was {result.AdviceKind} instead of IntroduceMethod." );
            }

            // TODO: #33060
            //if (!builder.Target.Compilation.Comparers.Default.Equals(
            //        result.Declaration.ForCompilation(builder.Advice.MutableCompilation), 
            //        builder.Target.Methods.OfName("Method").Single()))
            //{
            //    throw new InvalidOperationException($"Declaration was not correct.");
            //}
        }

        [Template]
        public int Method()
        {
            Console.WriteLine( "Aspect code." );

            return meta.Proceed();
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