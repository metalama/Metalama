using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Issue32302;

public class MyAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        var stringBuilder = new InterpolatedStringBuilder();
        stringBuilder.AddExpression( meta.Target.Method.Name );
        Console.WriteLine( stringBuilder.ToExpression().Value );

        return meta.Proceed();
    }
}

// <target>
public class C
{
    [MyAspect]
    private void M() { }
}