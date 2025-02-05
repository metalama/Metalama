using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AddAspect.TwoAspectsOfSameType
{
    [AttributeUsage( AttributeTargets.All, AllowMultiple = true )]
    internal class Aspect : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            Console.WriteLine( $"{meta.AspectInstance.SecondaryInstances.Length} other instance(s)" );

            return meta.Proceed();
        }
    }

    // <target>
    internal class TargetCode
    {
        [Aspect]
        [Aspect]
        private int Method( int a )
        {
            return a;
        }
    }
}