// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.ExtensionPoints;
using System;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_Arguments_DroppedWithSideEffect_Refused;

// The argument list of the new call is empty, and the source call passes an argument that can have a side effect. No argument of the new call
// can evaluate it, so the factory refuses the request.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
        => builder.TestRedirectCalls(
            "Format",
            ( (INamedType) TypeFactory.GetType( typeof(Interceptors) ) ).Methods.OfName( "FormatNothing" ).Single(),
            new TestRedirectionOptions { Arguments = "" } );
}

internal static class Source
{
    public static string Format( string value ) => value;
}

internal static class Interceptors
{
    public static string FormatNothing() => "intercepted";
}

// <target>
[Redirect]
internal static class Program
{
    public static void TestMain()
    {
        var value = "a";
        Console.WriteLine( Source.Format( value ) );
        Console.WriteLine( Source.Format( value.ToUpperInvariant() ) );
    }
}
