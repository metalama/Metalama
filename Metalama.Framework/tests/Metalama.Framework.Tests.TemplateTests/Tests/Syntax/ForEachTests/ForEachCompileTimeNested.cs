using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.ForEachTests.ForEachCompileTimeNested
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var array = meta.CompileTime( Enumerable.Range( 1, 2 ) );

            foreach (var n in array)
            {
                foreach (var p in meta.Target.Parameters)
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