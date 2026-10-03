// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.ExtensionPoints;
using System;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_Arguments_DroppedWithSideEffect_Before;

// The argument list of the new call omits the first two arguments of the source call, which can have side effects. They are evaluated into
// discards before the next argument, in their source order. The next argument is a constant, which the target converts to another type.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
        => builder.TestRedirectCalls(
            "Format",
            ( (INamedType) TypeFactory.GetType( typeof(Interceptors) ) ).Methods.OfName( "FormatLast" ).Single(),
            new TestRedirectionOptions { Arguments = "argument:2" } );
}

internal static class Source
{
    public static string Format( string first, string second, int third ) => $"{first} {second} {third}";
}

internal static class Interceptors
{
    public static string FormatLast( long third ) => $"intercepted {third}";
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
    public static void TestMain() => Console.WriteLine( Source.Format( Values.Get( "a" ), Values.Get( "b" ), 3 ) );
}
