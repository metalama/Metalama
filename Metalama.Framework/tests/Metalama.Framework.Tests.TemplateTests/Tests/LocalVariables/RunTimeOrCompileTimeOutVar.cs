using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.LocalVariables.RunTimeOrCompileTimeOutVar;

[CompileTime]
internal class Aspect
{
    [TestTemplate]
    private dynamic? Template()
    {
        if (meta.Target.Parameters.Single().Value > 0)
        {
            var s = meta.CompileTime( "0" );
            int.TryParse( s, out var i );
            i++;
            Console.WriteLine( $"i={i}" );
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