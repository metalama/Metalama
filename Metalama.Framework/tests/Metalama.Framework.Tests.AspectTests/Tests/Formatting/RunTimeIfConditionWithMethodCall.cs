// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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