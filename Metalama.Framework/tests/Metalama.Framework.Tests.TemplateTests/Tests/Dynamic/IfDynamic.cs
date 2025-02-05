using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code.SyntaxBuilders;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Dynamic.IfDynamic;

[CompileTime]
internal class Aspect
{
    [TestTemplate]
    private dynamic? Template()
    {
        if (meta.Proceed()) { }

        var b = new ExpressionBuilder();
        b.AppendExpression( true );

        if (b.ToValue()) { }

        return default;
    }
}

// <target>
internal class TargetCode
{
    private bool Method()
    {
        return true;
    }
}