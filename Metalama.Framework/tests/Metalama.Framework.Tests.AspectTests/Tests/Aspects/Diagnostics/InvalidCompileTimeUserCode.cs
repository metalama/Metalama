using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Diagnostics.InvalidCompileTimeUserCode
{
    public class ErrorAttribute : OverrideMethodAspect
    {
        public override void BuildAspect( IAspectBuilder<IMethod> builder )
        {
#if TESTRUNNER // Avoid the code to be parsed in the IDE.
            builder.BadMethod();
#endif
        }

        public override dynamic? OverrideMethod()
        {
            throw new NotImplementedException( "This code should not be emitted." );
        }
    }

    // <target>
    internal class TargetClass
    {
        [Error]
        public static int Add( int a, int b )
        {
            return a + b;
        }
    }
}