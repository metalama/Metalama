#if TEST_OPTIONS
// @IgnoredDiagnostic(CS1998)
#endif

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Async.NormalTemplate.AsyncMethod_CrossAssembly
{
    public class Aspect : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            Console.WriteLine( "Before" );
            var result = meta.Proceed();
            Console.WriteLine( "After" );

            return result;
        }
    }
}