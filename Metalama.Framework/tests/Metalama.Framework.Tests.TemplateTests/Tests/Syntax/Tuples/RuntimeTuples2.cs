using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.Tuples.RunTimeTuples2
{
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var t = ( 1, 2, 3 );
            Console.WriteLine( t.Item3 );

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