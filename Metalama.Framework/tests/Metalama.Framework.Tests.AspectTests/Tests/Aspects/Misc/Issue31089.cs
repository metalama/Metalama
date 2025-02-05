using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.Issue31089;

public class MyAspect : TypeAspect
{
    [Introduce]
    public void Method()
    {
        var stringBuilder = new InterpolatedStringBuilder();
        stringBuilder.AddText( "MachineName=" );
        stringBuilder.AddExpression( Environment.MachineName );

        Console.WriteLine( stringBuilder.ToValue() );
    }
}

[MyAspect]
internal class C { }