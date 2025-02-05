using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.IntegrationTests.Aspects.Overrides.Methods.Simple_TwoOverrides;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(OuterOverrideAttribute), typeof(InnerOverrideAttribute) )]

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Methods.Simple_TwoOverrides
{
    /*
     * Tests two OverrideMethod aspect with trivial template on methods with trivial bodies.
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
        public void VoidMethod()
        {
            Console.WriteLine( "This is the original method." );
        }

        [InnerOverride]
        [OuterOverride]
        public int Method( int x )
        {
            Console.WriteLine( $"This is the original method." );

            return x;
        }

        [InnerOverride]
        [OuterOverride]
        public T? GenericMethod<T>( T? x )
        {
            Console.WriteLine( "This is the original method." );

            return x;
        }
    }
}