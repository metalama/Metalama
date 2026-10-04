// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.ExtensionPoints;
using System;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_Arguments_Reordered_Trivia;

// The new argument list reorders the arguments of the source call. The comments attached to the source arguments move with them.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.TestRedirectCalls(
            "Format",
            TypeFactory.GetNamedType( typeof(Interceptors) ).Methods.OfName( "FormatReversed" ).Single(),
            new TestRedirectionOptions { Arguments = "argument:1; argument:0" } );
    }
}

internal static class Source
{
    public static string Format( string first, string second ) => $"{first} {second}";
}

internal static class Interceptors
{
    public static string FormatReversed( string second, string first ) => $"{second} {first}";
}

// <target>
[Redirect]
internal static class Program
{
    public static void TestMain()
    {
        Console.WriteLine(
            Source.Format(
                /* before first */ "a" /* after first */,
                /* before second */ "b" /* after second */ ) );
    }
}
