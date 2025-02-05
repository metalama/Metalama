using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.Do.BreakInRunTimeDo;

internal class Aspect
{
    [TestTemplate]
    private dynamic? Template()
    {
        var i = 0;

        do
        {
            i++;

            break;
        }
        while (i < meta.Target.Parameters.Count);

        Console.WriteLine( "Test result = " + i );

        var result = meta.Proceed();

        return result;
    }
}

internal class TargetCode
{
    private int Method( int a )
    {
        return a;
    }
}