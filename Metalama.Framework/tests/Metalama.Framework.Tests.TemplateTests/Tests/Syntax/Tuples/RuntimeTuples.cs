using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.Tuples.RunTimeTuples1
{
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            Tuple<string, int> tuple = new( "string", 0 );
            Console.WriteLine( tuple.Item1 );

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