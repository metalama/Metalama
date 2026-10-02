// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.ExtensionPoints;
using System;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_ReducedExtension;

// A call to a classic extension method in its reduced form passes its receiver as the first argument of the target. The call in the static
// form has no receiver, so the factory refuses the request with this receiver mode.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
        => builder.TestRedirectCalls(
            "Shout",
            ( (INamedType) TypeFactory.GetType( typeof(Interceptors) ) ).Methods.OfName( "Shout" ).Single(),
            new TestRedirectionOptions { ReceiverMode = "FirstArgument" } );
}

internal static class StringExtensions
{
    public static string Shout( this string s ) => s.ToUpperInvariant();
}

internal static class Interceptors
{
    public static string Shout( string s ) => $"intercepted {s}";
}

// <target>
[Redirect]
internal static class Program
{
    public static void TestMain()
    {
        Console.WriteLine( "hello".Shout() );
        Console.WriteLine( StringExtensions.Shout( "static form" ) );
    }
}
