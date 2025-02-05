using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.DynamicParameter_Virtual;

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
    protected virtual void CalledTemplate( dynamic a, dynamic b, int c, int d, [CompileTime] int e )
    {
        Console.WriteLine( $"called template a={a} b={b} c={c} d={d} e={e}" );
    }
}

internal class DerivedAspect : Aspect
{
    protected override void CalledTemplate( dynamic a, dynamic b, int c, int d, [CompileTime] int e )
    {
        Console.WriteLine( $"derived template a={a} b={b} c={c} d={d} e={e}" );
    }
}

// <target>
internal class TargetCode
{
    [Aspect]
    private void Method1( int x, int y ) { }

    [DerivedAspect]
    private void Method2( int y, int x ) { }
}