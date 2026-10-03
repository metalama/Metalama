// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.ExtensionPoints;
using System;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_Arguments_Cast;

// The source method takes a long, and the target takes an object. The argument is an int, so it is cast to long first, as the source call site
// converts it, and the target receives a boxed long. The program output shows the type of the boxed value.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
        => builder.TestRedirectCalls(
            "Describe",
            ( (INamedType) TypeFactory.GetType( typeof(Interceptors) ) ).Methods.OfName( "Describe" ).Single(),
            new TestRedirectionOptions { Arguments = "argument:0 as System.Int64" } );
}

internal static class Source
{
    public static string Describe( long value ) => $"source {value}";
}

internal static class Interceptors
{
    public static string Describe( object value ) => $"intercepted {value.GetType().Name}";
}

// <target>
[Redirect]
internal static class Program
{
    public static void TestMain()
    {
        var value = 5;
        Console.WriteLine( Source.Describe( value ) );
    }
}
