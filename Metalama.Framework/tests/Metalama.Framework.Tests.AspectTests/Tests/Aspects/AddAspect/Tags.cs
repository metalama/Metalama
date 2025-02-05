using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AddAspect.Tags
{
    internal class Aspect : MethodAspect
    {
        public override void BuildAspect( IAspectBuilder<IMethod> builder )
        {
            builder.Override( nameof(OverrideMethod), tags: new { Friend = "Bernard" } );
        }

        [Template]
        private dynamic? OverrideMethod()
        {
            Console.WriteLine( (string?)meta.Tags["Friend"] );

            return meta.Proceed();
        }
    }

    internal class TargetCode
    {
        // <target>
        [Aspect]
        private int Method( int a )
        {
            return a;
        }
    }
}