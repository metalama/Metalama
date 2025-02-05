using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.DynamicParameter;

internal class Aspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        base.BuildAspect( builder );

        builder.Override( nameof(OverrideMethod) );
    }

    [Template]
    public dynamic? OverrideMethod( dynamic x, dynamic y )
    {
        CalledTemplate( 0, x, y, 1, 2 );

        return default;
    }

    [Template]
    private void CalledTemplate( dynamic a, dynamic b, int c, int d, [CompileTime] int e )
    {
        Console.WriteLine( $"called template a={a} b={b} c={c} d={d} e={e}" );
    }
}

// <target>
internal class TargetCode
{
    [Aspect]
    private void Method( int x, int y ) { }
}