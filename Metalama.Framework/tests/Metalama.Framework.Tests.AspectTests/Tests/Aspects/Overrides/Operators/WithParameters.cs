using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Operators.WithParameters
{
    /*
     * Tests that simple cases of operator overriding work correctly.
     */

    public class OverrideAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            foreach (var o in builder.Target.Methods.OfKind( MethodKind.Operator ))
            {
                var templateName = o.OperatorKind.GetCategory() switch
                {
                    OperatorCategory.Unary => nameof(UnaryOperatorTemplate),
                    OperatorCategory.Binary => nameof(BinaryOperatorTemplate),
                    OperatorCategory.Conversion => nameof(ConversionOperatorTemplate),
                    _ => throw new Exception()
                };

                builder.With( o ).Override( templateName );
            }
        }

        [Template]
        public dynamic? BinaryOperatorTemplate( dynamic? x, dynamic? y )
        {
            Console.WriteLine( $"Overriding binary operator {meta.Target.Method.OperatorKind}({x}, {y})" );

            return meta.Proceed();
        }

        [Template]
        public dynamic? UnaryOperatorTemplate( dynamic? x )
        {
            Console.WriteLine( $"Overriding unary operator {meta.Target.Method.OperatorKind}({x})" );

            return meta.Proceed();
        }

        [Template]
        public dynamic? ConversionOperatorTemplate( dynamic? x )
        {
            Console.WriteLine( $"Overriding conversion operator {meta.Target.Method.OperatorKind}({x})" );

            return meta.Proceed();
        }
    }

    // <target>
    [Override]
    internal class TargetClass
    {
        public static TargetClass operator +( TargetClass a, TargetClass b )
        {
            Console.WriteLine( $"This is the original operator." );

            return new TargetClass();
        }

        public static TargetClass operator -( TargetClass a )
        {
            Console.WriteLine( $"This is the original operator." );

            return new TargetClass();
        }

        public static explicit operator TargetClass( int x )
        {
            Console.WriteLine( $"This is the original operator." );

            return new TargetClass();
        }
    }
}