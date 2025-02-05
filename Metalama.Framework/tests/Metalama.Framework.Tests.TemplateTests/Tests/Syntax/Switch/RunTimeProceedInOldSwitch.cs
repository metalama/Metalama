using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.Switch.ProceedInOldSwitchRunTime
{
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var i = 1;

            switch (i)
            {
                case 0:
                    Console.WriteLine( "0" );

                    break;

                case 1:
                    var x = meta.Proceed();

                    break;
            }

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