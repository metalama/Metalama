// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Generic;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Diagnostics.CompileTimeSideEffect_MethodCallOnField;

// ILLEGITIMATE: Calling a method on a compile-time field (declared outside the run-time conditional block)
// as a statement inside a run-time conditional block. The method mutates compile-time state
// that is visible outside the block.

internal class Aspect : OverrideMethodAspect
{
    private readonly List<string> _items = new();

    public override dynamic? OverrideMethod()
    {
        var x = meta.Target.Parameters[0].Value;

        if ( x != null )
        {
            _items.Add( "something" ); // ERROR: compile-time side effect on field in run-time conditional block
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
