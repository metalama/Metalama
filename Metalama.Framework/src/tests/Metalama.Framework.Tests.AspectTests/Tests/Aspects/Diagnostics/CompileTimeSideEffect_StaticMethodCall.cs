// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Generic;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Diagnostics.CompileTimeSideEffect_StaticMethodCall;

// ILLEGITIMATE: A static compile-time method call used as a statement inside a run-time conditional block.
// The call may have side effects on compile-time state that is visible outside the block.
// Example: calling a static method that mutates shared compile-time state.

[CompileTime]
internal static class CompileTimeHelper
{
    public static readonly List<string> Items = new();

    public static void AddItem( string item )
    {
        Items.Add( item );
    }
}

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        var x = meta.Target.Parameters[0].Value;

        if ( x != null )
        {
            CompileTimeHelper.AddItem( "something" ); // ERROR: compile-time side effect in run-time conditional block
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
