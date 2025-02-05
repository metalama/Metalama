using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.Cast.RunTimeCastOfCompileTimeValue;

[CompileTime]
internal class Aspect
{
    [TestTemplate]
    private dynamic? Template()
    {
        var parameter = (TargetCode)meta.Target.Parameters[0];

        return null;
    }
}

internal class TargetCode
{
    private string Method( string a )
    {
        return a;
    }
}