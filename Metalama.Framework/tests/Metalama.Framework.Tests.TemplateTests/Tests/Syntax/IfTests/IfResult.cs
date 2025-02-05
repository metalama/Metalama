#pragma warning disable CS8600, CS8603
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.IfTests.IfResult
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic Template()
        {
            var result = meta.Proceed();

            if (result == null)
            {
                return "";
            }

            return result;
        }
    }

    internal class TargetCode
    {
        private string Method( object a )
        {
            return a?.ToString();
        }
    }
}