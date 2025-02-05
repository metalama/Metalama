using System.Collections.Generic;
using System.Linq;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.Lambdas.RuntimeLinqDiscard
{
    internal class Aspect
    {
        [TestTemplate]
        private dynamic Template()
        {
            var list = new List<int>();

            return list.Where( _ => true ).Where( _ => true ).Count();
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