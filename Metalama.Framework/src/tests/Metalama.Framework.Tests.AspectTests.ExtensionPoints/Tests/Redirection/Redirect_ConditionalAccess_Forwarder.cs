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

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_ConditionalAccess_Forwarder;

// A call in a conditional access is redirected through a forwarder: an extension method that the linker generates and that calls the static
// interceptor. The receiver stays in the conditional access, so a null receiver still skips the call. The last call is not in a conditional
// access, so it calls the interceptor directly.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
        => builder.TestRedirectCalls(
            "Hello",
            ( (INamedType) TypeFactory.GetType( typeof(Interceptors) ) ).Methods.OfName( "InterceptedHello" ).Single(),
            new TestRedirectionOptions { ReceiverMode = "FirstArgument" } );
}

internal class Greeter
{
    public string Hello( string name ) => $"hello {name}";
}

internal static class Interceptors
{
    public static string InterceptedHello( Greeter greeter, string name ) => $"intercepted hello {name}";
}

// <target>
[Redirect]
internal static class Program
{
    public static void TestMain()
    {
        Greeter? missing = null;
        var greeter = new Greeter();

        Console.WriteLine( missing?.Hello( "a" ) ?? "null" );
        Console.WriteLine( greeter?.Hello( "b" ) ?? "null" );
        Console.WriteLine( new Greeter().Hello( "c" ) );
    }
}
