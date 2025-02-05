using System.Collections.Generic;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.RunTimeOutVarArg
{
    class Aspect
    {
        [TestTemplate]
        dynamic? Template()
        {
            var d = new Dictionary<int,int>();
            d.Add(0, 5);
            d.TryGetValue( 0, out var x );

            return null;
        }
    }

    class TargetCode
    {
        int Method(int a)
        {
            return a;
        }
    }
}