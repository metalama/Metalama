using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.Cast.CastDynamicToIExpression;

[CompileTime]
internal class Aspect
{
    [TestTemplate]
    private dynamic Template()
    {
        var parameter = (IExpression)meta.Target.Parameters[0].Value!;

        return parameter;
    }
}

internal class TargetCode
{
    private string Method( string a )
    {
        return a;
    }
}