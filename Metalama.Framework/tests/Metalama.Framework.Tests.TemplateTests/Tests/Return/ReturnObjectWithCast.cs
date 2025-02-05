using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.ReturnStatements.ReturnObjectWithCast
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            object? x = meta.Target.Parameters[0].Value;

            return x;
        }
    }

    internal class TargetCode
    {
        // <target>
        private int Method( int a )
        {
            return a;
        }
    }
}