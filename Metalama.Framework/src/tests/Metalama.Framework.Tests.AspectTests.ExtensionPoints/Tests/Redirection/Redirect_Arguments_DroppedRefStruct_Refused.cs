// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.ExtensionPoints;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_Arguments_DroppedRefStruct_Refused;

// A dropped value of a ref struct type cannot be passed to CallSiteHelper, because a ref struct cannot be a type argument, so the redirection is
// refused.

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
    public static string Format( string first, Token second ) => first;
}

internal static class Interceptors
{
    public static string FormatFirst( string first ) => $"intercepted {first}";
}

internal ref struct Token
{
    public static Token Create()
    {
        Console.WriteLine( "creating a token" );

        return default;
    }
}

// <target>
[Redirect]
internal static class Program
{
    public static void TestMain() => Console.WriteLine( Source.Format( "a", Token.Create() ) );
}
