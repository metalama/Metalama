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
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_Arguments_ThreeDroppedPacked;

// Three consecutive dropped values are packed into one tuple literal, in their source order, and evaluated before the kept value.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
        => builder.TestRedirectCalls(
            "Format",
            ( (INamedType) TypeFactory.GetType( typeof(Interceptors) ) ).Methods.OfName( "FormatLast" ).Single(),
            new TestRedirectionOptions { Arguments = "argument:3" } );
}

internal static class Source
{
    public static string Format( string first, string second, string third, int fourth ) => $"{first} {second} {third} {fourth}";
}

internal static class Interceptors
{
    public static string FormatLast( int fourth ) => $"intercepted {fourth}";
}

internal static class Values
{
    public static string Get( string value )
    {
        Console.WriteLine( $"evaluating {value}" );

        return value;
    }
}

// <target>
[Redirect]
internal static class Program
{
    public static void TestMain() => Console.WriteLine( Source.Format( Values.Get( "a" ), Values.Get( "b" ), Values.Get( "c" ), 4 ) );
}
