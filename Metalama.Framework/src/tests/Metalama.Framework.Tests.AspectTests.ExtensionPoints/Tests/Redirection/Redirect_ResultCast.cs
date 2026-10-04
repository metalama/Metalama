// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.ExtensionPoints;
using System;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_ResultCast;

// The target returns a wider type than the source method, so the new call is cast to the return type of the source method.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
        => builder.TestRedirectCalls(
            "Get",
            TypeFactory.GetNamedType( typeof(Interceptors) ).Methods.OfName( "Get" ).Single(),
            new TestRedirectionOptions { CastResult = true } );
}

internal static class Source
{
    public static int Get() => 1;
}

internal static class Interceptors
{
    public static long Get() => 2;
}

// <target>
[Redirect]
internal static class Program
{
    public static void TestMain()
    {
        var value = Source.Get();
        Console.WriteLine( value );
    }
}
