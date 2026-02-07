// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Generic;
using System.Linq;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Diagnostics.CompileTimeSideEffect_MethodCallOnInnerLocal;

// LEGITIMATE: Calling a method on a compile-time local that is declared INSIDE the run-time conditional block.
// The local's state does not leak outside the block, so this is safe.
// The compile-time local is created unconditionally (at compile time), and the method call
// only modifies state that cannot be observed outside the block.

internal class Aspect : TypeAspect
{
    [Introduce( WhenExists = OverrideStrategy.Override, Name = "ToString" )]
    public string IntroducedToString()
    {
        var fields = meta.Target.Type.FieldsAndProperties.Where( f => !f.IsImplicitlyDeclared && !f.IsStatic ).ToList();
        var x = 0; // run-time variable

        if ( x > 0 )
        {
            // innerList is a compile-time local declared inside the run-time conditional block.
            var innerList = meta.CompileTime( new List<string>() );

            foreach ( var field in fields )
            {
                innerList.Add( field.Name ); // OK: local declared inside the run-time conditional block
            }
        }

        return "done";
    }
}

// <target>
[Aspect]
internal class Foo
{
    public int X { get; set; }
    public string? Y { get; set; }
}
