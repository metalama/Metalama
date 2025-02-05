using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.Inheritance.Target_Struct_ImplementedInterfaceMethod
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
    internal struct Targets
    {
        private interface I
        {
            [Aspect]
            void M();
        }

        private struct S : I
        {
            public void M() { }
        }
    }
}