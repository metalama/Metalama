using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.Capture;

public class TheAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        var expression = ExpressionFactory.Capture( DateTime.Now );
        Console.WriteLine( $"Expression type = {expression.Type}" );

        return meta.Proceed();
    }
}

// <target>
internal class C
{
    [TheAspect]
    private void M() { }
}