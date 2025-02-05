using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System.Linq;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.Lambdas.CompileTimeLinqSimpleLambda
{
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var p = meta.Target.Parameters.Where( a => a.Name.Length > 8 ).Count();
            Console.WriteLine( p );

            return meta.Proceed();
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