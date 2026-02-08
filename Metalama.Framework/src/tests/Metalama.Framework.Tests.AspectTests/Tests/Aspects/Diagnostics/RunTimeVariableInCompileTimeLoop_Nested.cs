// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Linq;
using System.Text;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Diagnostics.RunTimeVariableInCompileTimeLoop_Nested;

// Test for https://github.com/metalama/Metalama/issues/829
// Variant where the compile-time method call is inside a compile-time foreach loop
// that is itself nested inside a run-time conditional block. The ancestor run-time
// conditional should still be detected.

internal class ToStringAttribute : TypeAspect
{
    [Introduce( WhenExists = OverrideStrategy.Override, Name = "ToString" )]
    public string IntroducedToString()
    {
        var stringBuilder = meta.CompileTime( new StringBuilder() );

        var fields = meta.Target.Type.FieldsAndProperties.Where( f => !f.IsImplicitlyDeclared && !f.IsStatic ).ToList();

        var i = 0; // run-time variable

        if ( i > 0 )
        {
            // Inside a run-time conditional block.
            foreach ( var field in fields )
            {
                // The foreach is compile-time, but the parent if is run-time.
                stringBuilder.Append( field.Name ); // ERROR: compile-time method call inside an ancestor run-time conditional block.
            }
        }

        return stringBuilder.ToString();
    }
}

// <target>
[ToString]
internal class Foo
{
    public int X { get; set; }

    public string? Y { get; set; }
}
