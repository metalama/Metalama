using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.LocalVariables.NameClashRunTimeFor
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var i = 0;
            Console.WriteLine( i );

            return meta.Proceed();
        }
    }

    internal class TargetCode
    {
        private int Method( int a, int b )
        {
            for (var i = 0; i < 5; i++)
            {
                Console.WriteLine( i );
            }

            return a + b;
        }
    }
}