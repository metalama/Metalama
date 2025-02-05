using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Operators.WithRunTimeAndCompileTimeParameters
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

                builder.With( o ).Override( templateName, args: new { operatorKind = o.OperatorKind } );
            }
        }

        [Template]
        public dynamic? BinaryOperatorTemplate( OperatorKind operatorKind, dynamic? x, dynamic? y )
        {
            Console.WriteLine( $"Overriding binary operator {operatorKind}({x}, {y})" );

            return meta.Proceed();
        }

        [Template]
        public dynamic? UnaryOperatorTemplate( OperatorKind operatorKind, dynamic? x )
        {
            Console.WriteLine( $"Overriding unary operator {operatorKind}({x})" );

            return meta.Proceed();
        }

        [Template]
        public dynamic? ConversionOperatorTemplate( OperatorKind operatorKind, dynamic? x )
        {
            Console.WriteLine( $"Overriding conversion operator {operatorKind}({x})" );

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