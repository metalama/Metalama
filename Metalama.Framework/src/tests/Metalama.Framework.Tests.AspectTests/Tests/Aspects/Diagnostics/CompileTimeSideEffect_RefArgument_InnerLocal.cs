// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Diagnostics.CompileTimeSideEffect_RefArgument_InnerLocal;

// LEGITIMATE: Passing a compile-time variable (declared INSIDE the run-time conditional block) as a ref argument.
// The variable's state does not leak outside the block, so no error should be reported.

internal class Aspect : OverrideMethodAspect
{
    private static void SetValue( ref int value )
    {
        value = 42;
    }

    public override dynamic? OverrideMethod()
    {
        var x = meta.Target.Parameters[0].Value;

        if ( x != null )
        {
            var counter = meta.CompileTime( 0 );
            SetValue( ref counter ); // OK: counter is declared inside the run-time conditional block
            Console.WriteLine( counter );
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
