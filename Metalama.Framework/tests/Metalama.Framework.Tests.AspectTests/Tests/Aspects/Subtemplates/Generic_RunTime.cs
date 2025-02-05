using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.Generic_RunTime;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        CalledTemplate<int>();

        return default;
    }

    [Template]
    private void CalledTemplate<T>()
    {
        Console.WriteLine( $"called template T={typeof(T)}" );
    }
}

// <target>
internal class TargetCode
{
    [Aspect]
    private void Method() { }
}