using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.While.RunTimeOrCompileTimeWhile;

internal class Aspect
{
    [TestTemplate]
    private dynamic? Template()
    {
        var i = 0;

        while (true)
        {
            i++;

            if (i >= meta.Target.Parameters.Count)
            {
                break;
            }
        }

        Console.WriteLine( "Test result = " + i );

        return meta.Proceed();
    }
}

internal class TargetCode
{
    private int Method( int a )
    {
        return a;
    }
}