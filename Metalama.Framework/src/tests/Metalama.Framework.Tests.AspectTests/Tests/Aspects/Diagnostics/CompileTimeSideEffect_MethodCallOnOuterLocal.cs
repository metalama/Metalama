// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Generic;
using System.Linq;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Diagnostics.CompileTimeSideEffect_MethodCallOnOuterLocal;

// ILLEGITIMATE: Calling a method on a compile-time local (declared outside the run-time conditional block)
// as a statement inside a run-time conditional block. The method call is assumed to have side effects
// that mutate the compile-time variable, and this mutation would be conditionally executed based on
// a run-time condition, leading to incorrect compile-time state.

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        var list = meta.CompileTime( new List<string>() );
        var x = meta.Target.Parameters[0].Value;

        if ( x != null )
        {
            list.Add( "something" ); // ERROR: compile-time side effect in run-time conditional block
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
