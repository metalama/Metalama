using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Tests.Templating.LocalFunctions.DynamicSignature;

[CompileTime]
internal class Aspect
{
    [TestTemplate]
    private dynamic? Template()
    {
        dynamic? TheLocalFunction()
        {
            return meta.Proceed();
        }

        return TheLocalFunction();
    }
}

internal class TargetCode
{
    private int Method( int a )
    {
        return a;
    }
}