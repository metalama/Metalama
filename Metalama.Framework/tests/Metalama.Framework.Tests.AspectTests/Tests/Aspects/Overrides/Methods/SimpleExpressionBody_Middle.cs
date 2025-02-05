using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Methods.SimpleExpressionBody_Middle
{
    // Tests single OverrideMethod aspect with trivial template on methods with trivial bodies.

    public class OverrideAttribute : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            Console.WriteLine( "This is the overriding method." );
            var x = meta.Proceed();
            Console.WriteLine( "This is the overriding method." );

            return x;
        }
    }

    // <target>
    internal class TargetClass
    {
        [Override]
        public void TargetMethod_Void() => Console.WriteLine( "This is the original method." );

        [Override]
        public void TargetMethod_Void( int x, int y ) => Console.WriteLine( $"This is the original method {x} {y}." );

        [Override]
        public int TargetMethod_Int() => 42;

        [Override]
        public int TargetMethod_Int( int x, int y ) => x + y;

        [Override]
        public static void TargetMethod_Static() => Console.WriteLine( "This is the original static method." );

        [Override]
        public void TargetMethod_Out( out int x ) => x = 42;

        [Override]
        public void TargetMethod_Ref( ref int x ) => x = 42;

        [Override]
        public void TargetMethod_In( in DateTime x ) => Console.WriteLine( $"This is the original method {x}." );
    }
}