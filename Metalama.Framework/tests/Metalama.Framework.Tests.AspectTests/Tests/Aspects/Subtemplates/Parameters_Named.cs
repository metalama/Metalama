using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.Parameters_Named;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        CalledTemplate( b: 1, a: 2, d: 3, c: 4 );
        CalledTemplate();
        CalledTemplate( d: 4, b: 2 );

        meta.InvokeTemplate( nameof(CalledTemplate2), args: new { b = 1, a = 2 } );
        meta.InvokeTemplate( nameof(CalledTemplate2) );
        meta.InvokeTemplate( nameof(CalledTemplate2), args: new { b = 2 } );

        return meta.Proceed();
    }

    [Template]
    private void CalledTemplate( int a = -1, int b = -2, [CompileTime] int c = -3, [CompileTime] int d = -4 )
    {
        Console.WriteLine( $"called template a={a} b={b} c={c} d={d}" );
    }

    [Template]
    private void CalledTemplate2( [CompileTime] int a = -1, [CompileTime] int b = -2 )
    {
        Console.WriteLine( $"called template 2 a={a} b={b}" );
    }
}

internal class TargetCode
{
    // <target>
    [Aspect]
    private void M() { }
}