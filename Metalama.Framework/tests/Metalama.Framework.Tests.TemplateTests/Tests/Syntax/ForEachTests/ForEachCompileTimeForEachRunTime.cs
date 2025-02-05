using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.ForEachTests.ForEachCompileTimeForEachRunTime
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var array = Enumerable.Range( 1, 2 );

            foreach (var p in meta.Target.Parameters)
            {
                foreach (var n in array)
                {
                    if (p.Value <= n)
                    {
                        Console.WriteLine( "Oops " + p.Name + " <= " + n );
                    }
                }
            }

            var result = meta.Proceed();

            return result;
        }
    }

    internal class TargetCode
    {
        private int Method( int a, int b )
        {
            return a + b;
        }
    }
}