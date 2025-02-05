using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.ForTests.CompileTimeFor;

[CompileTime]
internal class Aspect
{
    [TestTemplate]
    private dynamic? Template()
    {
        for (var i = meta.CompileTime( 0 ); i < 3; i++)
        {
            Console.WriteLine( i );
        }

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