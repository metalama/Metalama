using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.LocalVariables.OutVarInRunTimeConditionalBlock_Error;

[CompileTime]
internal class Aspect
{
    private void M( out int i ) => i = 1;

    [TestTemplate]
    private dynamic? Template()
    {
        var i = meta.CompileTime( 0 );

        if (meta.Target.Parameters.Single().Value > 0)
        {
            M( out i );
        }

        Console.WriteLine( $"i={i}" );

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