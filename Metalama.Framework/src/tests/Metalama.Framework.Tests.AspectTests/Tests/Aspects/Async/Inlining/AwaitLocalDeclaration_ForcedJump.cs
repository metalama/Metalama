// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Threading.Tasks;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Async.Inlining.AwaitLocalDeclaration_ForcedJump;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod() => throw new NotSupportedException();

    public override async Task<dynamic?> OverrideAsyncMethod()
    {
        Console.WriteLine( "Before" );
        var result = await meta.ProceedAsync();
        Console.WriteLine( $"After: {result}" );

        return result;
    }
}

// <target>
internal class TargetCode
{
    // Target has early return - forces goto generation during inlining
    [Aspect]
    private async Task<int> AsyncMethod( int a )
    {
        if ( a == 0 )
        {
            return 0; // Early return
        }

        await Task.Yield();

        return a * 2;
    }
}
