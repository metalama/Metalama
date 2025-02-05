using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.While.BreakInRunTimeWhile
{
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var i = 0;

            while (i < meta.Target.Parameters.Count)
            {
                i++;

                break;
            }

            Console.WriteLine( "Test result = " + i );

            var result = meta.Proceed();

            return result;
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