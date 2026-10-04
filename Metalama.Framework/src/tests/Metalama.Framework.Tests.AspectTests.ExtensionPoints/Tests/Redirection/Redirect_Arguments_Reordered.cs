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

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_Arguments_Reordered;

// The parameters of the target are in another order than those of the source method. The arguments are passed by name and written in the
// order of the source call, so they are evaluated in the original order.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
        => builder.TestRedirectCalls(
            "Format",
            TypeFactory.GetNamedType( typeof(Interceptors) ).Methods.OfName( "Format" ).Single(),
            new TestRedirectionOptions { Arguments = "argument:1; argument:0" } );
}

internal static class Source
{
    public static string Format( string first, string second ) => $"{first} {second}";
}

internal static class Interceptors
{
    public static string Format( string second, string first ) => $"intercepted {first} {second}";
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
    public static void TestMain() => Console.WriteLine( Source.Format( Values.Get( "a" ), Values.Get( "b" ) ) );
}
