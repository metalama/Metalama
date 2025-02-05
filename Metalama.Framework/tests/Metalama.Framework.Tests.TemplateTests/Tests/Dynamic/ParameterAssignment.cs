using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Dynamic.ParameterAssignment
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var result = meta.Proceed();
            meta.Target.Parameters[0].Value = 5;

            return result;
        }
    }

    // <target>
    internal class TargetCode
    {
        private int Method( out int a )
        {
            a = 1;

            return 1;
        }
    }
}