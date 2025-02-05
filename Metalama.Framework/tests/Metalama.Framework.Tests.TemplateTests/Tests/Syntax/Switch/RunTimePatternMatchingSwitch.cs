using System;
using System.Linq;
using System.Collections.Generic;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.Switch.RuntimeatternMatchingSwitch
{
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var o = new object();

            switch (o)
            {
                case IEnumerable<object> a when a.Any():
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