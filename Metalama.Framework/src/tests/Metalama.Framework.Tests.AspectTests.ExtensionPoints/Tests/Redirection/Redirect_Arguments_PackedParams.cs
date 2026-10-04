// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @MainMethod(TestMain)
#endif

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.ExtensionPoints;
using System;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_Arguments_PackedParams;

// The parameters of the target are in another order, and the source calls pass the params argument in expanded form. The elements are packed into
// a collection expression, which is written as a named argument. They are evaluated in their source order.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
        => builder.TestRedirectCalls(
            "Sum",
            TypeFactory.GetNamedType( typeof(Interceptors) ).Methods.OfName( "Sum" ).Single(),
            new TestRedirectionOptions { Arguments = "argument:1; argument:0" } );
}

internal static class Source
{
    public static string Sum( string label, params int[] values ) => $"{label} {values.Sum()}";
}

internal static class Interceptors
{
    public static string Sum( int[] values, string label ) => $"intercepted {label} {values.Length} {values.Sum()}";
}

internal static class Values
{
    public static int Get( int value )
    {
        Console.WriteLine( $"evaluating {value}" );

        return value;
    }
}

// <target>
[Redirect]
internal static class Program
{
    public static void TestMain()
    {
        Console.WriteLine( Source.Sum( "a", Values.Get( 1 ), Values.Get( 2 ) ) );
        Console.WriteLine( Source.Sum( "b" ) );
    }
}
