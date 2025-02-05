using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Highlighting.IfStatements.RunTimeIfConditionWithMethodCall;

public class ForcedJumpOverrideAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        var x = meta.Proceed();

        if (new Random().Next() == 0)
        {
            Console.Write($"ForcedJump: randomly");
            return x;
        }

        Console.Write($"ForcedJump: normally");
        return x;
    }
}