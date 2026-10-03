// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Fabrics;
using Metalama.Framework.Tests.ExtensionPoints;
using System;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_Arguments_Reordered_Fabric;

// A project fabric redirects a call to a target whose parameters are in another order.

internal class Fabric : ProjectFabric
{
    public override void AmendProject( IProjectAmender amender )
        => amender.SelectTypes()
            .Where( t => t.Name == "Program" )
            .TestRedirectCalls(
                "Format",
                "Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_Arguments_Reordered_Fabric.Interceptors",
                "Format",
                new TestRedirectionOptions { Arguments = "argument:1; argument:0" } );
}

internal static class Source
{
    public static string Format( string first, string second ) => $"{first} {second}";
}

internal static class Interceptors
{
    public static string Format( string second, string first ) => $"intercepted {first} {second}";
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
internal static class Program
{
    public static void TestMain() => Console.WriteLine( Source.Format( Values.Get( "a" ), Values.Get( "b" ) ) );
}
