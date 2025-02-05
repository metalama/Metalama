using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.LocalVariables.NameClashCompileTimeIf
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var n = meta.Target.Parameters.Count;        // build-time
            object? y = meta.Target.Parameters[0].Value; // run-time

            if (n == 1)
            {
                var x = 0;
                Console.WriteLine( x );
            }

            if (y == null)
            {
                var x = 1;
                Console.WriteLine( x );
            }

            if (n == 1)
            {
                var x = 2;
                Console.WriteLine( x );
            }

            if (y == null)
            {
                var x = 3;
                Console.WriteLine( x );
            }

            return meta.Proceed();
        }
    }

    internal class TargetCode
    {
        private int Method( int a )
        {
            return a;
        }
    }
}