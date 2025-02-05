using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.Lock.CompileTimeLock
{
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            lock (meta.Target.Compilation)
            {
                return meta.Proceed();
            }
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