using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.ForEachTests.ForEachContinueCompileTime
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var i = meta.CompileTime( 0 );

            foreach (var p in meta.Target.Parameters)
            {
                if (p.Name.Length <= 1)
                {
                    continue;
                }

                i++;
            }

            Console.WriteLine( i );

            var result = meta.Proceed();

            return result;
        }
    }

    internal class TargetCode
    {
        private int Method( int a, int bb )
        {
            return a + bb;
        }
    }
}