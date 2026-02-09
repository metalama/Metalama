// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Diagnostics.CompileTimeSideEffect_RefArgument;

// ILLEGITIMATE: Passing a compile-time variable (declared outside the run-time conditional block) as a ref argument
// inside a run-time conditional block. The ref parameter modifies the variable, which is a compile-time side effect.
// LAMA0108 should be reported.

internal class Aspect : OverrideMethodAspect
{
    private static void SetValue( ref int value )
    {
        value = 42;
    }

    public override dynamic? OverrideMethod()
    {
        var counter = meta.CompileTime( 0 );
        var x = meta.Target.Parameters[0].Value;

        if ( x != null )
        {
            SetValue( ref counter ); // ERROR LAMA0108: ref modifies compile-time variable in run-time conditional block
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
