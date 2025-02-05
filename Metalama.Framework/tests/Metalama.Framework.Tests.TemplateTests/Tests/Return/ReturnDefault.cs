#pragma warning disable CS8600, CS8603
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.ReturnStatements.ReturnDefault
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic Template()
        {
            try
            {
                var result = meta.Proceed();

                return result;
            }
            catch
            {
                return default;
            }
        }
    }

    internal class TargetCode
    {
        // <target>
        private int Method( int a )
        {
            return 42 / a;
        }
    }
}