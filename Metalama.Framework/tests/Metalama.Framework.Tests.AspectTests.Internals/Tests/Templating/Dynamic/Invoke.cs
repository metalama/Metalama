using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Dynamic.Invoke
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var method = meta.This.Method;
            method();
            method.Invoke();

            return default;
        }
    }

    // <target>
    internal class TargetCode
    {
        private void Method() { }
    }
}