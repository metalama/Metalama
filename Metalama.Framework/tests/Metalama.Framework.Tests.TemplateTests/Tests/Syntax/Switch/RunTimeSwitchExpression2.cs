using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.RunTimeSwitchExpression2
{
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            object o = new();

            var y = o switch
            {
                int i => meta.Target.Parameters[0].Name.Length,
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