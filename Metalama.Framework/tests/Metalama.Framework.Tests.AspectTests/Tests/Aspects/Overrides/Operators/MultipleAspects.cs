using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.IntegrationTests.Aspects.Overrides.Operators.MultipleAspects;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(OuterOverrideAttribute), typeof(InnerOverrideAttribute) )]

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Operators.MultipleAspects
{
    /*
     * Tests that multiple aspects overriding an operator work correctly.
     */

    public class InnerOverrideAttribute : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            Console.WriteLine( "This is the inner overriding template method." );

            return meta.Proceed();
        }
    }

    public class OuterOverrideAttribute : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            Console.WriteLine( "This is the outer overriding template method." );

            return meta.Proceed();
        }
    }

    // <target>
    internal class TargetClass
    {
        [InnerOverride]
        [OuterOverride]
        public static TargetClass operator +( TargetClass a, TargetClass b )
        {
            Console.WriteLine( $"This is the original operator." );

            return new TargetClass();
        }

        [InnerOverride]
        [OuterOverride]
        public static TargetClass operator -( TargetClass a )
        {
            Console.WriteLine( $"This is the original operator." );

            return new TargetClass();
        }

        [InnerOverride]
        [OuterOverride]
        public static explicit operator TargetClass( int x )
        {
            Console.WriteLine( $"This is the original operator." );

            return new TargetClass();
        }
    }
}