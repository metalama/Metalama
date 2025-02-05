using System;
using System.Threading.Tasks;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.RedundantReturn_Async;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        CalledTemplateTask();
        CalledTemplateValueTask();

        return default;
    }

    [Template]
    private async Task CalledTemplateTask()
    {
        Console.WriteLine( "Task" );

        return;
    }

    [Template]
    private async ValueTask CalledTemplateValueTask()
    {
        Console.WriteLine( "ValueTask" );

        return;
    }
}

// <target>
internal class TargetCode
{
    [Aspect]
    private void Method() { }
}