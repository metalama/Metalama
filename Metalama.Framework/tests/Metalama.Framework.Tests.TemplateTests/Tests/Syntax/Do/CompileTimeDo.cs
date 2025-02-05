using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.Do.CompileTimeDo;

internal class Aspect
{
    [TestTemplate]
    private dynamic? Template()
    {
        var i = meta.CompileTime( 0 );

        do
        {
            i++;
        }
        while (i < meta.Target.Method.Name.Length);

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