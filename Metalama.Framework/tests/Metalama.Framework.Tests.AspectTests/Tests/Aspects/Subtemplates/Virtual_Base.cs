using System;
using System.Threading.Tasks;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.Virtual_Base;

#pragma warning disable CS1998 // Async method lacks 'await' operators

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( "virtual method" );

        return meta.Proceed();
    }

    public override async Task<dynamic?> OverrideAsyncMethod()
    {
        Console.WriteLine( "normal template" );

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        base.OverrideAsyncMethod();

        throw new Exception();
    }
}

// <target>
internal class TargetCode
{
    [Aspect]
    private async Task Method() { }
}