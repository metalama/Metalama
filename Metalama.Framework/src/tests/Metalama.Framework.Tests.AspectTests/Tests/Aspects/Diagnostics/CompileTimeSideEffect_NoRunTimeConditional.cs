// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Linq;
using System.Text;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Diagnostics.CompileTimeSideEffect_NoRunTimeConditional;

// LEGITIMATE: Compile-time method calls with side effects NOT inside any run-time conditional block.
// This is normal compile-time code and should work correctly.

internal class ToStringAttribute : TypeAspect
{
    [Introduce( WhenExists = OverrideStrategy.Override, Name = "ToString" )]
    public string IntroducedToString()
    {
        var stringBuilder = meta.CompileTime( new StringBuilder() );
        stringBuilder.Append( meta.Target.Type.Name );
        stringBuilder.Append( " " );

        var fields = meta.Target.Type.FieldsAndProperties.Where( f => !f.IsImplicitlyDeclared && !f.IsStatic ).ToList();

        var i = meta.CompileTime( 0 ); // compile-time variable (correct usage)

        foreach ( var field in fields )
        {
            if ( i > 0 )
            {
                stringBuilder.Append( ", " ); // OK: i is compile-time, so this if is compile-time
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
