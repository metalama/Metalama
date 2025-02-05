using System;
using System.Linq;
using System.Collections.Generic;
using Metalama.Framework.Code;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.Switch.MismatchScopePatternMatchingSwitch
{
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var o = new object();

            switch (o)
            {
                case IParameter p:
                    Console.WriteLine( "0" );

                    break;

                case IEnumerable<object> e when e.Count() == meta.Target.Parameters.Count:
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