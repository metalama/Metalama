using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.Conditional;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        var templates = Templates.Create();

        templates?.CalledTemplate();

        return default;
    }
}

internal class Templates : ITemplateProvider
{
    [CompileTime]
    public static Templates? Create() => new();

    [Template]
    public void CalledTemplate()
    {
        Console.WriteLine( "called template" );
    }
}

// <target>
internal class TargetCode
{
    [Aspect]
    private void Method() { }
}