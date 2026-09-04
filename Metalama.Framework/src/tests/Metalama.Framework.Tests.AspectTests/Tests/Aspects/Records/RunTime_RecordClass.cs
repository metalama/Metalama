// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Text;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Records.RunTime_RecordClass;

public class OverrideAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceMethod( nameof(IntroducedEquals), whenExists: OverrideStrategy.Override, args: new { T = builder.Target } );
        builder.IntroduceMethod( nameof(IntroducedGetHashCode), whenExists: OverrideStrategy.Override );
        builder.IntroduceMethod( nameof(IntroducedToString), whenExists: OverrideStrategy.Override );
        builder.IntroduceMethod( nameof(IntroducedPrintMembers), whenExists: OverrideStrategy.Override );
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
}

// <target>
[Override]
internal record Transformed( int X, string Y );

internal record Twin( int X, string Y );

internal static class Program
{
    public static void TestMain()
    {
        var a = new Transformed( 1, "x" );
        var b = new Transformed( 1, "x" );
        var c = new Transformed( 2, "x" );
        var twin = new Twin( 1, "x" );

        Console.WriteLine( a.ToString() );
        Console.WriteLine( $"ToStringMatchesCompiler: {a.ToString().Replace( "Transformed", "Twin" ) == twin.ToString()}" );
        Console.WriteLine( $"Equals(same): {a.Equals( b )}" );
        Console.WriteLine( $"Equals(different): {a.Equals( c )}" );
        Console.WriteLine( $"ObjectEquals: {a.Equals( (object) b )}" );
        Console.WriteLine( $"Operator==: {a == b}" );
        Console.WriteLine( $"Operator!=: {a != c}" );
        Console.WriteLine( $"HashEqual: {a.GetHashCode() == b.GetHashCode()}" );
        Console.WriteLine( $"HashDifferent: {a.GetHashCode() != c.GetHashCode()}" );

        var withCopy = a with { X = 3 };
        Console.WriteLine( $"With: {withCopy}" );

        a.Deconstruct( out var x, out var y );
        Console.WriteLine( $"Deconstruct: {x}, {y}" );

        // This is the last use of the variable, because the nullable analysis of a comparison with null
        // makes the compiler treat the receiver as possibly null afterwards.
        Console.WriteLine( $"Equals(null): {a.Equals( null )}" );
    }
}
