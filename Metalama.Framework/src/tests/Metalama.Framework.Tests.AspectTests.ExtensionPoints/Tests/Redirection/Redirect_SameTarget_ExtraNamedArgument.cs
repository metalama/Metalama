// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.ExtensionPoints;
using System;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_SameTarget_ExtraNamedArgument;

// A call is redirected to the same method with an additional named argument.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
        => builder.TestRedirectCalls(
            "Log",
            TypeFactory.GetNamedType( typeof(Logger) ).Methods.OfName( "Log" ).Single(),
            new TestRedirectionOptions { ExtraArguments = "origin=\"redirected\"" } );
}

internal static class Logger
{
    public static void Log( string message, string origin = "source" ) => Console.WriteLine( $"{origin}: {message}" );
}

// <target>
[Redirect]
internal static class Program
{
    public static void TestMain() => Logger.Log( "message" );
}
