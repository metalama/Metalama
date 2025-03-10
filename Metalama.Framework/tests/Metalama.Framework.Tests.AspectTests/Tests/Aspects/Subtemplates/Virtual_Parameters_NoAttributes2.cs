// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Threading.Tasks;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.Virtual_Parameters_NoAttributes2;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( "regular template" );

        CalledTemplate( 1, 2 );

        return meta.Proceed();
    }

    [Template]
    protected virtual void CalledTemplate( int i, [CompileTime] int j )
    {
        Console.WriteLine( $"called template i={i} j={j}" );
    }
}

internal class DerivedAspect : Aspect
{
    public override Task<dynamic?> OverrideAsyncMethod()
    {
        CalledTemplate( 3, 4 );

        return meta.ProceedAsync();
    }

    protected override void CalledTemplate( int i, int j )
    {
        Console.WriteLine( $"called template i={i} j={j}" );
    }
}

// <target>
internal class TargetCode
{
    [Aspect]
    private void Method1() { }

    [DerivedAspect]
    private void Method2() { }

    [DerivedAspect]
    private async Task Method3()
    {
        await Task.Yield();
    }
}