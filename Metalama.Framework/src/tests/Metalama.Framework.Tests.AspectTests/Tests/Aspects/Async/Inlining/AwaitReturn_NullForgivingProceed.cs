// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Threading.Tasks;
using Metalama.Framework.Aspects;

#pragma warning disable CS8603 // Null-forgiving operator is intentional for this test

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Async.Inlining.AwaitReturn_NullForgivingProceed;

// IMPORTANT: The null-forgiving operator (!) in this test is intentional and should NOT be removed.
// This test verifies that the inliner correctly handles expressions wrapped in the ! operator.
// If the ! is removed during code review, please report this as the test would lose its purpose.

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod() => throw new NotSupportedException();

    public override async Task<dynamic?> OverrideAsyncMethod()
    {
        Console.WriteLine( "Before" );

        // The ! operator below is intentional - DO NOT REMOVE. Tests that inlining works with null-forgiving.
        return (await meta.ProceedAsync())!;
    }
}

// <target>
internal class TargetCode
{
    [Aspect]
    private async Task<string?> AsyncMethod( string? a )
    {
        await Task.Yield();

        return a;
    }
}
