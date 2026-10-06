// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.ExtensionPoints;
using System;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_BindsToOtherOverload_DefaultsWritten;

// The target has an optional parameter, and an overload of the target without it exists. The rewritten call would bind to the overload, so the
// factory writes the default value of the optional parameter by name, and the call binds to the target.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
        => builder.TestRedirectCalls(
            "Print",
            TypeFactory.GetNamedType( typeof(Interceptors) ).Methods.OfName( "Print" ).Single( m => m.Parameters.Count == 2 ) );
}

internal static class Source
{
    public static void Print( string value ) => Console.WriteLine( $"source {value}" );
}

internal static class Interceptors
{
    public static void Print( string value ) => Console.WriteLine( $"overload {value}" );

    public static void Print( string value, int count = 1 ) => Console.WriteLine( $"intercepted {value} {count}" );
}

// <target>
[Redirect]
internal static class Program
{
    public static void TestMain() => Source.Print( "a" );
}
