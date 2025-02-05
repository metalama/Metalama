using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Operators.Simple
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
                builder.With( o ).Override( nameof(Template) );
            }
        }

        [Template]
        public dynamic? Template()
        {
            Console.WriteLine( "This is the override." );

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