// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Text;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Diagnostics.CompileTimeSideEffect_MixedSideEffects;

// ILLEGITIMATE: Multiple types of compile-time side effects in a run-time conditional block:
// 1. Compile-time assignment (LAMA0108)
// 2. Compile-time method call as statement (LAMA0288)
// Both should be reported.

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        var counter = meta.CompileTime( 0 );
        var sb = meta.CompileTime( new StringBuilder() );
        var x = meta.Target.Parameters[0].Value;

        if ( x != null )
        {
            counter = 42; // ERROR LAMA0108: compile-time variable assignment in run-time conditional block
            sb.Append( "hello" ); // ERROR LAMA0288: compile-time method call in run-time conditional block
        }

        return meta.Proceed();
    }
}

// <target>
internal class Target
{
    [Aspect]
    private void M( string? s ) { }
}
