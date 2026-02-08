// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Text;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Diagnostics.CompileTimeSideEffect_NestedRunTimeConditionals;

// ILLEGITIMATE: A compile-time variable (sb) is declared inside the outer run-time conditional block.
// A method call on it (sb.Append) is inside an inner (nested) run-time conditional block.
// The variable is "inner" to the outer block but "outer" to the inner block, so the side effect
// is visible outside the inner run-time conditional block. LAMA0288 should be reported.

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        var x = meta.Target.Parameters[0].Value;
        var y = meta.Target.Parameters[1].Value;

        if ( x != null )
        {
            var sb = meta.CompileTime( new StringBuilder() );

            if ( y != null )
            {
                sb.Append( "hello" ); // ERROR LAMA0288: sb is declared outside this inner run-time conditional block
            }
        }

        return meta.Proceed();
    }
}

// <target>
internal class Target
{
    [Aspect]
    private void M( string? s, string? t ) { }
}
