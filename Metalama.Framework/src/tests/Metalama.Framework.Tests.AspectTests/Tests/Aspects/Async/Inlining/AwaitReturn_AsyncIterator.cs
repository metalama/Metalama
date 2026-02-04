// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(NET5_0_OR_GREATER)
#endif

#if NET5_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Async.Inlining.AwaitReturn_AsyncIterator;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod() => throw new NotSupportedException();

    public override async IAsyncEnumerable<dynamic?> OverrideAsyncEnumerableMethod()
    {
        Console.WriteLine( "Before" );

        await foreach ( var item in meta.ProceedAsyncEnumerable() )
        {
            yield return item;
        }
    }
}

// <target>
internal class TargetCode
{
    [Aspect]
    private async IAsyncEnumerable<int> AsyncIteratorMethod( int count )
    {
        for ( var i = 0; i < count; i++ )
        {
            await Task.Delay( 1 );
            yield return i;
        }
    }
}

#endif
