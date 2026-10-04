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

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_Arguments_DroppedWithSideEffect_After;

// The argument list of the new call omits the last argument of the source call, which can have a side effect. No argument follows it, so it is
// evaluated into a discard after the previous argument, whose value a pattern variable holds. The second call drops a constant, which is not
// evaluated.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
        => builder.TestRedirectCalls(
            "Format",
            ( (INamedType) TypeFactory.GetType( typeof(Interceptors) ) ).Methods.OfName( "FormatFirst" ).Single(),
            new TestRedirectionOptions { Arguments = "argument:0" } );
}

internal static class Source
{
    public static string Format( string first, string second ) => $"{first} {second}";
}

internal static class Interceptors
{
    public static string FormatFirst( string first ) => $"intercepted {first}";
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
    public static void TestMain()
    {
        Console.WriteLine( Source.Format( Values.Get( "a" ), Values.Get( "b" ) ) );
        Console.WriteLine( Source.Format( Values.Get( "c" ), "d" ) );
    }
}
