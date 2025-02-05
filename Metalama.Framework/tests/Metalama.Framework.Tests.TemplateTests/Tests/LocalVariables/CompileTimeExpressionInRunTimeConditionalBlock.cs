using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.LocalVariables.CompileTimeExpressionInRunTimeConditionalBlock;

[CompileTime]
internal class Aspect
{
    [TestTemplate]
    private dynamic? Template()
    {
        if (meta.Target.Parameters.Single().Value > 0)
        {
            var i = meta.CompileTime( 0 );
            var j = meta.CompileTime( 0 );
            ( i, j ) = ( 3, 4 );
            Console.WriteLine( $"i={i} j={j}" );
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