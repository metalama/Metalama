using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.SyntaxBuilders.Switch.TemplateInvocation;

public class TheAspect : TypeAspect
{
    [Introduce]
    public void A()
    {
        var statement = StatementFactory.FromTemplate( new Framework.Aspects.TemplateInvocation( nameof(Template) ) );
        meta.InsertStatement( statement );
    }

    [Template]
    private void Template()
    {
        Console.WriteLine( "Hello, world." );
    }
}

// <target>
[TheAspect]
internal class C { }