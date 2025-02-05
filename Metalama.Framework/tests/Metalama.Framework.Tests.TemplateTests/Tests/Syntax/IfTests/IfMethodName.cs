using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.IfTests.IfMethodName
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var b = meta.CompileTime( 0 );

            if (meta.Target.Method.Name == "Method")
            {
                b = 1;
            }
            else
            {
                b = 2;
            }

            Console.WriteLine( b );

            return meta.Proceed();
        }
    }

    internal class TargetCode
    {
        private void Method() { }
    }
}