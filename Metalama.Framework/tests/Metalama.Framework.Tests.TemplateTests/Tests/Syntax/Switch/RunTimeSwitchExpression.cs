#pragma warning disable CS8509

using System;
using System.Linq;
using System.Collections.Generic;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.RunTimeSwitchExpression
{
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var x = DateTime.Now.DayOfWeek switch
            {
                DayOfWeek.Monday => "Spaghetti",
                DayOfWeek.Tuesday => "Salad",
                _ => "McDonald"
            };

            object o = new();

            var y = o switch
            {
                IEnumerable<object> enumerable when enumerable.Count() > meta.Target.Parameters.Count => -1,
                IEnumerable<object> enumerable2 => enumerable2.Count()
            };

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