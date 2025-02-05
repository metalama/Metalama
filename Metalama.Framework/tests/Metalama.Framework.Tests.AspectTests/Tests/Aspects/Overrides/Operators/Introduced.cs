using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.IntegrationTests.Aspects.Overrides.Operators.Introduced;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(OverrideAttribute), typeof(IntroductionAttribute) )]

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Operators.Introduced
{
    /*
     * Tests that operator overriding work correctly for introduced operators.
     */

    public class OverrideAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            foreach (var o in builder.Target.Methods.OfKind( MethodKind.Operator ))
            {
                builder.With( o ).Override( nameof(Template) );
            }
        }

        [Template]
        public dynamic? Template()
        {
            Console.WriteLine( $"Overriding the operator {meta.Target.Method.OperatorKind}." );

            return meta.Proceed();
        }
    }

    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.IntroduceUnaryOperator( nameof(UnaryOperatorTemplate), builder.Target, builder.Target, OperatorKind.UnaryNegation );

            builder.IntroduceBinaryOperator(
                nameof(BinaryOperatorTemplate),
                builder.Target,
                TypeFactory.GetType( typeof(int) ),
                builder.Target,
                OperatorKind.Addition );

            builder.IntroduceConversionOperator(
                nameof(ConversionOperatorTemplate),
                TypeFactory.GetType( typeof(int) ),
                builder.Target );
        }

        [Template]
        public dynamic? UnaryOperatorTemplate( dynamic? x )
        {
            Console.WriteLine( $"Unary operator {meta.Target.Method.OperatorKind}({x})" );

            return meta.Proceed();
        }

        [Template]
        public dynamic? BinaryOperatorTemplate( dynamic? x, dynamic? y )
        {
            Console.WriteLine( $"Binary operator {meta.Target.Method.OperatorKind}({x}, {y})" );

            return meta.Proceed();
        }

        [Template]
        public dynamic? ConversionOperatorTemplate( dynamic? x )
        {
            Console.WriteLine( $"Conversion operator {meta.Target.Method.OperatorKind}({x})" );

            return meta.Proceed();
        }
    }

    // <target>
    [Introduction]
    [Override]
    internal class TargetClass { }
}