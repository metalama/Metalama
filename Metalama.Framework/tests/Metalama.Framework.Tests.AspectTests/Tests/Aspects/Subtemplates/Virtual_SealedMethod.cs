using System;
using System.Threading.Tasks;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.Virtual_SealedMethod;

#pragma warning disable CS1998 // Async method lacks 'await' operators

internal class Aspect : OverrideMethodAspect
{
    public sealed override dynamic? OverrideMethod()
    {
        Console.WriteLine( "virtual method" );

        return meta.Proceed();
    }

    public override async Task<dynamic?> OverrideAsyncMethod()
    {
        Console.WriteLine( "normal template" );

        OverrideMethod();

        throw new Exception();
    }
}

// <target>
internal class TargetCode
{
    [Aspect]
    private async Task Method()
    {
        await Task.Yield();
    }
}