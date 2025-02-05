using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.Inheritance.ImplementedGenericInterfaceMethod
{
    [Inheritable]
    internal class Aspect : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            Console.WriteLine( "Overridden!" );

            return meta.Proceed();
        }
    }

    // <target>
    internal class Targets
    {
        private interface I<T>
        {
            [Aspect]
            void M( T x );
        }

        private class C : I<int>
        {
            public void M( int x ) { }

            // This one should not be transformed.
            public void M( string x ) { }
        }
    }
}