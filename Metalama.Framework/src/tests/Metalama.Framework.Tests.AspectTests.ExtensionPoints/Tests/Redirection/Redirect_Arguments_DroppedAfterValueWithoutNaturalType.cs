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

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_Arguments_DroppedAfterValueWithoutNaturalType;

// The kept arguments, the default literal and a collection expression, have no natural type, so the type arguments of CallSiteHelper.DropAfter
// are written, and each kept value is converted to the type of the parameter as in the original call.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
        => builder.TestRedirectCalls(
            "Format",
            TypeFactory.GetNamedType( typeof(Interceptors) ).Methods.OfName( "FormatFirst" ).Single(),
            new TestRedirectionOptions { Arguments = "argument:0" } );
}

internal static class Source
{
    public static string Format( List<int>? first, string second ) => $"{first?.Count} {second}";
}

internal static class Interceptors
{
    public static string FormatFirst( List<int>? first ) => $"intercepted {first?.Count ?? -1}";
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
        Console.WriteLine( Source.Format( default, Values.Get( "x" ) ) );
        Console.WriteLine( Source.Format( [1, 2], Values.Get( "y" ) ) );
    }
}
