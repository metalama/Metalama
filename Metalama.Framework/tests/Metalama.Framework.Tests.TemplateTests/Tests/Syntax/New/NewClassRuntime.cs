using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.New.RunTimeNewClass
{
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var o = new TargetCode();
            Console.WriteLine( o.GetType().ToString() );

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