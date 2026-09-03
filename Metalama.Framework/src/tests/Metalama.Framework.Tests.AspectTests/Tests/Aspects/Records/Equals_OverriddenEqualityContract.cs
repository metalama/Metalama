// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Records.Equals_OverriddenEqualityContract;

/// <summary>
/// The aspect overrides two synthesized members, one of which the other reads. The C# compiler compares the
/// <c>EqualityContract</c> of the two instances in <c>Equals</c>, and the property is virtual, so the comparison is a
/// call that reaches the advice on both instances. The console output of the getter shows the two calls.
/// </summary>
public class OverrideAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceMethod( nameof(IntroducedEquals), whenExists: OverrideStrategy.Override, args: new { T = builder.Target } );
        builder.With( builder.Target.Properties.Single( p => p.Name == "EqualityContract" ) ).Override( nameof(IntroducedProperty) );
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
            Console.WriteLine( "  (the getter of EqualityContract runs)" );

            return meta.Proceed();
        }
    }
}

// <target>
[Override]
internal record Target( int X );

internal static class Program
{
    public static void TestMain()
    {
        var a = new Target( 1 );
        var b = new Target( 1 );

        Console.WriteLine( "Equals:" );
        Console.WriteLine( $"  result: {a.Equals( b )}" );
    }
}
