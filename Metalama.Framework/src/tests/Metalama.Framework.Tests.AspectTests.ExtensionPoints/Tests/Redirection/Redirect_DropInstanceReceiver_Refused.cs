// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.ExtensionPoints;
using System;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_DropInstanceReceiver_Refused;

// A call to an instance method cannot be redirected with the receiver mode Drop, because the receiver would not be evaluated. The factory refuses
// the request.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
        => builder.TestRedirectCalls(
            "Greet",
            TypeFactory.GetNamedType( typeof(Interceptors) ).Methods.OfName( "Greet" ).Single() );
}

internal class Greeter
{
    public void Greet( string s ) => Console.WriteLine( s );
}

internal static class Interceptors
{
    public static void Greet( string s ) => Console.WriteLine( $"intercepted {s}" );
}

// <target>
[Redirect]
internal static class Program
{
    public static void TestMain() => new Greeter().Greet( "x" );
}
