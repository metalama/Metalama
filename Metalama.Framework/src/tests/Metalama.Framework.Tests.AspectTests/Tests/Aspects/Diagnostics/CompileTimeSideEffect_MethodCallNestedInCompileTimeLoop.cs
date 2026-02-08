// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Linq;
using System.Text;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Diagnostics.CompileTimeSideEffect_MethodCallNestedInCompileTimeLoop;

// ILLEGITIMATE: Compile-time method call on a local declared outside the run-time conditional block,
// where the call is inside a compile-time foreach that is itself nested inside a run-time if.
// The compile-time foreach loop is unrolled, so the method call is still inside the run-time conditional block.

internal class Aspect : TypeAspect
{
    [Introduce( WhenExists = OverrideStrategy.Override, Name = "ToString" )]
    public string IntroducedToString()
    {
        var stringBuilder = meta.CompileTime( new StringBuilder() );
        var fields = meta.Target.Type.FieldsAndProperties.Where( f => !f.IsImplicitlyDeclared && !f.IsStatic ).ToList();

        var i = 0; // run-time variable

        if ( i > 0 )
        {
            foreach ( var field in fields )
            {
                stringBuilder.Append( field.Name ); // ERROR: compile-time side effect in ancestor run-time conditional block
            }
        }

        return stringBuilder.ToString();
    }
}

// <target>
[Aspect]
internal class Foo
{
    public int X { get; set; }

    public string? Y { get; set; }
}
