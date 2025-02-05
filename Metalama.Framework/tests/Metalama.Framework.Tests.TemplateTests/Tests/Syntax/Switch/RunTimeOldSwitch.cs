using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.Switch.DefaultInOldSwitchRunTime
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

                default:
                    Console.WriteLine( "Default" );

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