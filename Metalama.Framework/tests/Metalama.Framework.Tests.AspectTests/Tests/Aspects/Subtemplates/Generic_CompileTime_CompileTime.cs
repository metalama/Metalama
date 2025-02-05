using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.Generic_CompileTime_CompileTime;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        CalledTemplate<IType?>( meta.Target.Type );

        return default;
    }

    [Template]
    private void CalledTemplate<[CompileTime] T>( T x )
    {
        Console.WriteLine( $"called template T={typeof(T)} x={x}" );
    }
}

// <target>
internal class TargetCode
{
    [Aspect]
    private void Method() { }
}