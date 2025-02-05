using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.TemplateProvider_CrossAssembly;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( "regular template" );
        var templates = new Templates();
        templates.Template( 1 );
        meta.InvokeTemplate( nameof(Templates.Template), templates, new { i = 2 } );

        return default;
    }
}

internal class TargetCode
{
    // <target>
    [Aspect]
    private void Method() { }
}