using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Samples.SimpleLogging
{
    public class LogAttribute : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            Console.WriteLine( meta.Target.Method.ToDisplayString() + " started." );

            try
            {
                var result = meta.Proceed();

                Console.WriteLine( meta.Target.Method.ToDisplayString() + " succeeded." );

                return result;
            }
            catch (Exception e)
            {
                Console.WriteLine( meta.Target.Method.ToDisplayString() + " failed: " + e.Message );

                throw;
            }
        }
    }

    // <target>
    internal class TargetClass
    {
        [Log]
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