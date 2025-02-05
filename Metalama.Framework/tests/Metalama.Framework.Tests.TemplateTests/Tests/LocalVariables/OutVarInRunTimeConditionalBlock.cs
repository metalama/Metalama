using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.LocalVariables.OutVarInRunTimeConditionalBlock;

[CompileTime]
internal class Aspect
{
    private void M( out int i, out int j ) => ( i, j ) = ( 1, 2 );

    [TestTemplate]
    private dynamic? Template()
    {
        if (meta.Target.Parameters.Single().Value > 0)
        {
            var i = meta.CompileTime( 0 );
            M( out i, out var j );
            j++;
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