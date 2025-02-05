using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Linq;

#pragma warning disable CS0618 // IAdviceResult.AspectBuilder is obsolete

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Operators.AdviceResult_Override_Existing
{
    public class TestAspect : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            var result =
                builder.IntroduceBinaryOperator(
                    nameof(Operator),
                    builder.Target,
                    TypeFactory.GetType( SpecialType.Int32 ),
                    TypeFactory.GetType( SpecialType.Int32 ),
                    OperatorKind.Addition,
                    whenExists: OverrideStrategy.Override );

            if (result.Outcome != AdviceOutcome.Override)
            {
                throw new InvalidOperationException( $"Outcome was {result.Outcome} instead of Override." );
            }

            if (result.AdviceKind != AdviceKind.IntroduceOperator)
            {
                throw new InvalidOperationException( $"AdviceKind was {result.AdviceKind} instead of IntroduceOperator." );
            }

            if (!builder.Target.Compilation.Comparers.Default.Equals(
                    result.Declaration.ForCompilation( builder.Advice.MutableCompilation ),
                    builder.Target.Methods.OfName( "op_Addition" ).Single() ))
            {
                throw new InvalidOperationException( $"Declaration was not correct." );
            }
        }

        [Template]
        public int Operator( dynamic? x, dynamic? y )
        {
            Console.WriteLine( "Aspect code." );

            return meta.Proceed();
        }
    }

    // <target>
    [TestAspect]
    public class TargetClass
    {
        public static int operator +( TargetClass x, int y )
        {
            Console.WriteLine( "Original code." );

            return y;
        }
    }
}