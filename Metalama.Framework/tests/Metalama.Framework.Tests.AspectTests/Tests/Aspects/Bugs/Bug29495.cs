using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Bugs.Bug29495
{
    internal class Aspect : OverrideMethodAspect
    {
        public MyEnum Value { get; set; }

        public override dynamic? OverrideMethod()
        {
            Console.WriteLine( Value.ToString() );

            return meta.Proceed();
        }
    }

    // <target>
    internal class TargetCode
    {
        [Aspect( Value = MyEnum.B )]
        private int Method( int a )
        {
            return a;
        }
    }
}