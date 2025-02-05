using System;
using System.Threading.Tasks;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.Virtual_Empty;

#pragma warning disable CS1998 // Async method lacks 'await' operators

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( "virtual method" );

        OverrideAsyncMethod();

        return meta.Proceed();
    }
}

// <target>
internal class TargetCode
{
    [Aspect]
    private async Task Method() { }
}