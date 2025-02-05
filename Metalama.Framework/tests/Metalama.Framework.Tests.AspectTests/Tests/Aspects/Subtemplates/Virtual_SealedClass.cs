using System;
using System.Threading.Tasks;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.Virtual_SealedClass;

#pragma warning disable CS1998 // Async method lacks 'await' operators

internal sealed class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( "virtual method" );

        return meta.Proceed();
    }

    public override async Task<dynamic?> OverrideAsyncMethod()
    {
        Console.WriteLine( "normal template" );

        meta.InvokeTemplate( nameof(OverrideMethod) );

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