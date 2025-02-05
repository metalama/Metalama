using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Diagnostics.SkipWithoutError
{
    public class SkippedAttribute : OverrideMethodAspect
    {
        public override void BuildAspect( IAspectBuilder<IMethod> builder )
        {
            base.BuildAspect( builder );

            builder.SkipAspect();
        }

        public override dynamic? OverrideMethod()
        {
            throw new NotImplementedException( "This code should not be emitted." );
        }
    }

    // <target>
    internal class TargetClass
    {
        [Skipped]
        public static int Add( int a, int b )
        {
            if (a == 0)
            {
                throw new ArgumentOutOfRangeException( nameof(a) );
            }

            return a + b;
        }
    }
}