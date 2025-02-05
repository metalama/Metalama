using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.Cast.CastCompileTimeToIExpression;

[CompileTime]
internal class Aspect
{
    [TestTemplate]
    private dynamic Template()
    {
        var expression = (IExpression)meta.Target.Parameters[0];

        return expression.Value!.ToString();
    }
}

internal class TargetCode
{
    private string Method( string a )
    {
        return a;
    }
}