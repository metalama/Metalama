// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Threading.Tasks;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Async.Inlining.AwaitReturn_ChainedOverrides;

internal class Aspect1 : OverrideMethodAspect
{
    public override dynamic? OverrideMethod() => throw new NotSupportedException();

    public override async Task<dynamic?> OverrideAsyncMethod()
    {
        Console.WriteLine( "Aspect1 Before" );
        var result = await meta.ProceedAsync();
        Console.WriteLine( "Aspect1 After" );

        return result;
    }
}

internal class Aspect2 : OverrideMethodAspect
{
    public override dynamic? OverrideMethod() => throw new NotSupportedException();

    public override async Task<dynamic?> OverrideAsyncMethod()
    {
        Console.WriteLine( "Aspect2 Before" );
        var result = await meta.ProceedAsync();
        Console.WriteLine( "Aspect2 After" );

        return result;
    }
}

// <target>
internal class TargetCode
{
    // Two aspects on same async method - both should inline
    [Aspect1]
    [Aspect2]
    private async Task<int> AsyncMethod( int a )
    {
        await Task.Yield();

        return a * 2;
    }
}
