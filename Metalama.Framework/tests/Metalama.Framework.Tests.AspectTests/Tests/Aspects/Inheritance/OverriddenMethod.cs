using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.Inheritance.InheritedMethodAttribute
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
        private class BaseClass
        {
            [Aspect]
            public virtual void M() { }
        }

        private class DerivedClass : BaseClass
        {
            public override void M() { }
        }
    }
}