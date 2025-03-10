// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Threading.Tasks;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.Virtual_SealedMethod;

#pragma warning disable CS1998 // Async method lacks 'await' operators

internal class Aspect : OverrideMethodAspect
{
    public sealed override dynamic? OverrideMethod()
    {
        Console.WriteLine( "virtual method" );

        return meta.Proceed();
    }

    public override async Task<dynamic?> OverrideAsyncMethod()
    {
        Console.WriteLine( "normal template" );

        OverrideMethod();

        throw new Exception();
    }
}

// <target>
internal class TargetCode
{
    [Aspect]
    private async Task Method()
    {
        await Task.Yield();
    }
}