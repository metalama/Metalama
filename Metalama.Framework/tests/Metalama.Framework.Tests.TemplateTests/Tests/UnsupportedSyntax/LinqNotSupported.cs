using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.UnsupportedSyntax.LinqNotSupported
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic Template()
        {
            var result = meta.Proceed();

#pragma warning disable CS0618
            var list = from i in new int[] { 1, 2, 3 }
                       select i * i;
#pragma warning restore CS0618
            if (result == null)
            {
                result =
                    from i in list
                    from i2 in list
                    let ii = i * i
                    where true
                    orderby i, i2 descending
                    join j in list on i equals j
                    join j2 in list on i equals j2 into g
                    group i by i2;
            }

            return result;
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