using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.DynamicCallWithArgs;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        meta.InvokeTemplate( nameof(CalledTemplateSimple), args: new { a = 42 } );
        meta.InvokeTemplate( new TemplateInvocation( nameof(CalledTemplateInvocation), arguments: new { a = 0, b = 1 } ), new { a = 42, c = 2 } );
        var templateProvider = new Templates();
        meta.InvokeTemplate( nameof(Templates.CalledTemplateSimple), templateProvider, new { a = 42 } );

        meta.InvokeTemplate(
            new TemplateInvocation( nameof(Templates.CalledTemplateInvocation), templateProvider, new { a = 0, b = 1 } ),
            new { a = 42, c = 2 } );

        return default;
    }

    [Template]
    private void CalledTemplateSimple( [CompileTime] int a )
    {
        Console.WriteLine( $"called template simple aspect a={a}" );
    }

    [Template]
    private void CalledTemplateInvocation( [CompileTime] int a, [CompileTime] int b, [CompileTime] int c )
    {
        Console.WriteLine( $"called template invocation aspect a={a} b={b} c={c}" );
    }
}

[CompileTime]
internal class Templates : ITemplateProvider
{
    [Template]
    public void CalledTemplateSimple( [CompileTime] int a )
    {
        Console.WriteLine( $"called template simple provider a={a}" );
    }

    [Template]
    public void CalledTemplateInvocation( [CompileTime] int a, [CompileTime] int b, [CompileTime] int c )
    {
        Console.WriteLine( $"called template invocation provider a={a} b={b} c={c}" );
    }
}

internal class TargetCode
{
    // <target>
    [Aspect]
    private void Method() { }
}