using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Tests.Templating.LocalFunctions.NameConflict_GenericParameter;

#pragma warning disable CS8321

[CompileTime]
internal class Aspect
{
    [TestTemplate]
    private dynamic? Template()
    {
        int TheLocalFunction<TParameter>()
        {
            return meta.Proceed();
        }

        return TheLocalFunction<int>();
    }
}

internal class TargetCode
{
    private int Method( int a )
    {
        void TParameter()
        {
            // Some conflict.
        }

        return a;
    }
}