// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.DynamicCallWithArgs_RunTime;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        meta.InvokeTemplate(nameof(CalledTemplate), args: new { a = 42, b = ExpressionFactory.Capture(13) });

        return default;
    }

    [Template]
    private void CalledTemplate(int a, int b)
    {
        Console.WriteLine($"called template a={a}, b={b}");
    }
}

internal class TargetCode
{
    // <target>
    [Aspect]
    private void Method() { }
}