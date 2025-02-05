using System;
using System.Collections.Generic;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.Generic_CompileTime_DynamicCall;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        meta.InvokeTemplate( nameof(CalledTemplate), args: new { T = typeof(int), i = 1 } );

        return default;
    }

    [Template]
    private void CalledTemplate<[CompileTime] T>( [CompileTime] int i )
    {
        Console.WriteLine( $"called template T={typeof(T)} i={i}" );

        meta.InvokeTemplate( nameof(CalledTemplate2), args: new { T = typeof(T) } );

        meta.InvokeTemplate( nameof(CalledTemplate2), args: new { T = typeof(T[]) } );

        meta.InvokeTemplate( nameof(CalledTemplate2), args: new { T = typeof(Dictionary<int, T>) } );

        meta.InvokeTemplate( nameof(CalledTemplate2), args: new { T = typeof(TargetCode) } );
    }

    [Template]
    private void CalledTemplate2<[CompileTime] T>()
    {
        Console.WriteLine( $"called template 2 T={typeof(T)}" );
    }
}

// <target>
internal class TargetCode
{
    [Aspect]
    private void Method() { }
}