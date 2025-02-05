using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.LocalVariables.NameClashRunTimeMultiVariable2
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
            var (i, j) = ( 4, 5 );

            return a + b;
        }
    }
}