// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Threading.Tasks;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Async.Inlining.NoInline_CompoundAssignment;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod() => throw new NotSupportedException();

    public override async Task<dynamic?> OverrideAsyncMethod()
    {
        Console.WriteLine( "Before" );
        var result = 10;
        result += await meta.ProceedAsync(); // Compound assignment - NOT inlineable
        Console.WriteLine( "After" );

        return result;
    }
}

// <target>
internal class TargetCode
{
    [Aspect]
    private async Task<int> AsyncMethod( int a )
    {
        await Task.Yield();

        return a;
    }
}
