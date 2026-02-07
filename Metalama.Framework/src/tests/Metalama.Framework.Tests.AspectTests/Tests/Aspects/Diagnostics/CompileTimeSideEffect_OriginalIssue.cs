// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Linq;
using System.Text;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Diagnostics.CompileTimeSideEffect_OriginalIssue;

// ILLEGITIMATE: This is the original issue from https://github.com/metalama/Metalama/issues/829.
// The 'i' variable is run-time (should have been meta.CompileTime(0)), causing the 'if (i > 0)' block
// to be a run-time conditional. Inside this block, stringBuilder.Append(", ") is a compile-time
// method call with side effects on a compile-time variable declared outside the block.

internal class ToStringAttribute : TypeAspect
{
    [Introduce( WhenExists = OverrideStrategy.Override, Name = "ToString" )]
    public string IntroducedToString()
    {
        var stringBuilder = meta.CompileTime( new StringBuilder() );
        stringBuilder.Append( meta.Target.Type.Name );
        stringBuilder.Append( " " );

        var fields = meta.Target.Type.FieldsAndProperties.Where( f => !f.IsImplicitlyDeclared && !f.IsStatic ).ToList();

        var i = 0; // This is a run-time variable, but should be meta.CompileTime(0).

        foreach ( var field in fields )
        {
            if ( i > 0 )
            {
                stringBuilder.Append( ", " ); // ERROR: compile-time side effect in run-time conditional block
            }

            stringBuilder.Append( field.Name );

            i++;
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
