using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Dynamic.DynamicReceiverThisInStaticContext
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var x = meta.This;

            return default;
        }
    }

    // <target>
    internal static class TargetCode
    {
        private static int Method( int a )
        {
            return a;
        }
    }
}