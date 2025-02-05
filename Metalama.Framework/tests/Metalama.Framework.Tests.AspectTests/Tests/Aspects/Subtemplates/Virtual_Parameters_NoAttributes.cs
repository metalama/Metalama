using System;
using System.Threading.Tasks;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.Virtual_Parameters_NoAttributes;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( "regular template" );

        CalledTemplate<int>( 1, 2, 3 );

        return meta.Proceed();
    }

    [Template]
    protected virtual void CalledTemplate<[CompileTime] T>( int i, [CompileTime] int j, T k )
    {
        Console.WriteLine( $"called template i={i} j={j} k={k}" );
    }
}

internal class DerivedAspect : Aspect
{
    public override Task<dynamic?> OverrideAsyncMethod()
    {
        CalledTemplate<int>( 4, 5, 6 );

        return meta.ProceedAsync();
    }

    protected override void CalledTemplate<T>( int i, int j, T k )
    {
        Console.WriteLine( $"derived template i={i} j={j} k={k}" );
    }
}

// <target>
internal class TargetCode
{
    [Aspect]
    private void Method1() { }

    [DerivedAspect]
    private void Method2() { }

    [DerivedAspect]
    private async Task Method3()
    {
        await Task.Yield();
    }
}