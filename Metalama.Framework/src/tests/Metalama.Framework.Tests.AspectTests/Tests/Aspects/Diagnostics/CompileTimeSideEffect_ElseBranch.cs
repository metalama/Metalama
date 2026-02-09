// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Generic;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Diagnostics.CompileTimeSideEffect_ElseBranch;

// ILLEGITIMATE: Compile-time method call in the else branch of a run-time conditional.
// The else branch is also conditionally executed based on a run-time condition.

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        var list = meta.CompileTime( new List<string>() );
        var x = meta.Target.Parameters[0].Value;

        if ( x != null )
        {
            list.Add( "not-null" ); // ERROR: compile-time side effect in run-time conditional block
        }
        else
        {
            list.Add( "null" ); // ERROR: compile-time side effect in run-time conditional block
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
