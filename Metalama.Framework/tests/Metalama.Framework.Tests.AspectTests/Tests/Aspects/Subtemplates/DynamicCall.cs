using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.DynamicCall;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( "regular template" );
        meta.InvokeTemplate( nameof(CalledTemplateSimple) );
        meta.InvokeTemplate( new TemplateInvocation( nameof(CalledTemplateInvocation) ) );
        var templateProvider = TemplateProvider.FromInstance( new Templates() );
        meta.InvokeTemplate( nameof(Templates.CalledTemplateSimple), templateProvider );
        meta.InvokeTemplate( new TemplateInvocation( nameof(Templates.CalledTemplateInvocation), templateProvider ) );

        return default;
    }

    [Template]
    private void CalledTemplateSimple()
    {
        Console.WriteLine( "called template simple aspect" );
    }

    [Template]
    private void CalledTemplateInvocation()
    {
        Console.WriteLine( "called template invocation aspect" );
    }
}

[CompileTime]
internal class Templates : ITemplateProvider
{
    [Template]
    public void CalledTemplateSimple()
    {
        Console.WriteLine( "called template simple provider" );
    }

    [Template]
    public void CalledTemplateInvocation()
    {
        Console.WriteLine( "called template invocation provider" );
    }
}

internal class TargetCode
{
    // <target>
    [Aspect]
    private void Method() { }
}