// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Linq;
using System.Text;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Records.RunTime_OverriddenProperty;

/// <summary>
/// The aspect overrides the auto-property and every materializable synthesized member. The getter of the property
/// writes a line to the console, so the output shows which materialized bodies reach the advice on the property.
/// </summary>
/// <remarks>
/// <c>Equals</c> and <c>GetHashCode</c> read the backing field, which is the lowest layer of the property, so they do
/// not reach the advice. <c>PrintMembers</c> and <c>Deconstruct</c> call the getter, which is what the C# compiler
/// does, so they do reach it.
/// </remarks>
public class OverrideAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceMethod( nameof(IntroducedEquals), whenExists: OverrideStrategy.Override, args: new { T = builder.Target } );
        builder.IntroduceMethod( nameof(IntroducedGetHashCode), whenExists: OverrideStrategy.Override );
        builder.IntroduceMethod( nameof(IntroducedToString), whenExists: OverrideStrategy.Override );
        builder.IntroduceMethod( nameof(IntroducedPrintMembers), whenExists: OverrideStrategy.Override );
        builder.IntroduceMethod( nameof(IntroducedDeconstruct), whenExists: OverrideStrategy.Override );
        builder.With( builder.Target.Properties.Single( p => p.Name == "X" ) ).Override( nameof(IntroducedProperty) );
    }

    [Template( Name = "Equals" )]
    public bool IntroducedEquals<[CompileTime] T>( T? other )
    {
        return meta.Proceed();
    }

    [Template( Name = "GetHashCode" )]
    public int IntroducedGetHashCode()
    {
        return meta.Proceed();
    }

    [Template( Name = "ToString" )]
    public string IntroducedToString()
    {
        return meta.Proceed()!;
    }

    [Template( Name = "PrintMembers" )]
    protected bool IntroducedPrintMembers( StringBuilder builder )
    {
        return meta.Proceed();
    }

    [Template( Name = "Deconstruct" )]
    public void IntroducedDeconstruct( out int X )
    {
        X = default;

        meta.Proceed();
    }

    [Template]
    public dynamic? IntroducedProperty
    {
        get
        {
            Console.WriteLine( "  (the getter of X runs)" );

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
internal record Transformed( int X );

internal static class Program
{
    public static void TestMain()
    {
        var a = new Transformed( 1 );
        var b = new Transformed( 1 );

        Console.WriteLine( "Equals:" );
        Console.WriteLine( $"  result: {a.Equals( b )}" );

        Console.WriteLine( "GetHashCode:" );
        Console.WriteLine( $"  equal instances have the same hash code: {a.GetHashCode() == b.GetHashCode()}" );

        Console.WriteLine( "ToString:" );
        Console.WriteLine( $"  result: {a}" );

        Console.WriteLine( "Deconstruct:" );
        a.Deconstruct( out var x );
        Console.WriteLine( $"  result: {x}" );
    }
}
