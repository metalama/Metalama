using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Operators.Target_MultipleSignatures
{
    /*
     * Tests that overrides of multiple signatures of the same operator are transformed correct.
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
            if (meta.Target.Method.Parameters.Count == 2)
            {
                Console.WriteLine(
                    $"This is the override of ({meta.Target.Method.Parameters[0].Type}, {meta.Target.Method.Parameters[1].Type}) -> {meta.Target.Method.ReturnParameter.Type}." );
            }
            else
            {
                Console.WriteLine( $"This is the override of ({meta.Target.Method.Parameters[0].Type}) -> {meta.Target.Method.ReturnParameter.Type}." );
            }

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

        public static TargetClass operator +( int a, TargetClass b )
        {
            Console.WriteLine( $"This is the original operator." );

            return new TargetClass();
        }

        public static TargetClass operator +( TargetClass a, int b )
        {
            Console.WriteLine( $"This is the original operator." );

            return new TargetClass();
        }

        public static explicit operator TargetClass( int x )
        {
            Console.WriteLine( $"This is the original operator." );

            return new TargetClass();
        }

        public static explicit operator int( TargetClass x )
        {
            Console.WriteLine( $"This is the original operator." );

            return 42;
        }
    }
}