using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.Cast.CompileTimeCastOfRunTimeValue;

[CompileTime]
internal class Aspect
{
    [TestTemplate]
    private dynamic? Template()
    {
        var parameter = (IParameter)meta.Target.Parameters[0].Value!;

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