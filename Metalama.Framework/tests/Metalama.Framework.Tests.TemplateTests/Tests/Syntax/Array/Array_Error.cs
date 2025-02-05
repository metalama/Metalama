using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Tests.Templating.Syntax.Array.Array_Error
{
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            // Error: cannot mix build-time and run-time.
            var a = new object[] { meta.Target.Compilation, meta.This };

            return default;
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