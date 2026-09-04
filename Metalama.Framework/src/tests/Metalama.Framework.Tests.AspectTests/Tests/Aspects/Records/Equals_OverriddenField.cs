// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Records.Equals_OverriddenField;

/// <summary>
/// The aspect overrides both a field and the synthesized <c>Equals</c>. Overriding a field promotes it to a property
/// whose backing field carries the value, and the materialized body must read that backing field, which is the lowest
/// layer of the member, rather than the promoted property, which carries the advice.
/// </summary>
public class OverrideAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceMethod( nameof(IntroducedEquals), whenExists: OverrideStrategy.Override, args: new { T = builder.Target } );
        builder.With( builder.Target.Fields.Single( f => f.Name == "Value" ) ).Override( nameof(IntroducedProperty) );
    }

    [Template( Name = "Equals" )]
    public bool IntroducedEquals<[CompileTime] T>( T? other )
    {
        return meta.Proceed();
    }

    [Template]
    public dynamic? IntroducedProperty
    {
        get
        {
            Console.WriteLine( "  (the getter of Value runs)" );

            return meta.Proceed();
        }

        set
        {
            meta.Proceed();
        }
    }
}

// <target>
[Override]
internal record Target
{
    public int Value;
}

internal static class Program
{
    public static void TestMain()
    {
        var a = new Target { Value = 1 };
        var b = new Target { Value = 1 };

        Console.WriteLine( "Equals:" );
        Console.WriteLine( $"  result: {a.Equals( b )}" );
    }
}
