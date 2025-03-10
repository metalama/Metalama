// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Threading.Tasks;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.Virtual_DynamicCall;

#pragma warning disable CS1998 // Async method lacks 'await' operators

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( "virtual method" );

        return meta.Proceed();
    }

    public override async Task<dynamic?> OverrideAsyncMethod()
    {
        Console.WriteLine( "normal template" );

        if (meta.Target.Parameters["condition"].Value)
        {
            meta.InvokeTemplate( nameof(OverrideMethod) );
        }
        else
        {
            meta.InvokeTemplate( nameof(OverrideMethod), this );
        }

        throw new Exception();
    }
}

internal class DerivedAspect : Aspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( "overridden virtual method" );

        return meta.Proceed();
    }
}

// <target>
internal class TargetCode
{
    [Aspect]
    private async Task Method1( bool condition )
    {
        await Task.Yield();
    }

    [DerivedAspect]
    private async Task Method2( bool condition )
    {
        await Task.Yield();
    }
}