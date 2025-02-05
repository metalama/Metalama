using Metalama.Framework.Code;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.RunTimeSwitchExpressionMismatch
{
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            object o = new();

            var y = o switch
            {
                IParameter p => 1,
                _ => 0
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