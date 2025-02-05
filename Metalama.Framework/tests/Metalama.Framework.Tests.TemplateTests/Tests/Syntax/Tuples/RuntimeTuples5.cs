using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.Tuples.RunTimeTuples
{
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var items = ( a: 1, b: 2, 3 );
            Console.WriteLine( items.a );

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