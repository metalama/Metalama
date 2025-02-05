using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.Inheritance.InheritedMethodInGenericTypettribute
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
        private class BaseClass<T>
        {
            [Aspect]
            public virtual T M( T a ) => a;
        }

        private class DerivedClass : BaseClass<int>
        {
            public override int M( int a ) => a + 1;
        }
    }
}