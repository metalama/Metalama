// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Threading.Tasks;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Async.Inlining.NoInline_FieldAssignment;

internal class Aspect : OverrideMethodAspect
{
    [Introduce]
    private int _lastResult;

    public override dynamic? OverrideMethod() => throw new NotSupportedException();

    public override async Task<dynamic?> OverrideAsyncMethod()
    {
        Console.WriteLine( "Before" );
        this._lastResult = await meta.ProceedAsync(); // Assignment to introduced field - NOT inlineable
        Console.WriteLine( "After" );

        return this._lastResult;
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
