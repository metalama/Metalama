using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.DynamicCallWithArgs_RunTime;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        meta.InvokeTemplate(nameof(CalledTemplate), args: new { a = 42, b = ExpressionFactory.Capture(13) });

        return default;
    }

    [Template]
    private void CalledTemplate(int a, int b)
    {
        Console.WriteLine($"called template a={a}, b={b}");
    }
}

internal class TargetCode
{
    // <target>
    [Aspect]
    private void Method() { }
}