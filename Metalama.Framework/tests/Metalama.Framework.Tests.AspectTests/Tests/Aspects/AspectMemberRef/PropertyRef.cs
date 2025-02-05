using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.AspectMemberRef.PropertyRef
{
    public class RetryAttribute : OverrideMethodAspect
    {
        public int Property { get; set; } = 5;

        public override dynamic? OverrideMethod()
        {
            Console.WriteLine( Property );

            return meta.Proceed();
        }
    }

    internal class Program
    {
        // <target>
        [Retry( Property = 10 )]
        private static int Foo()
        {
            return 0;
        }
    }
}