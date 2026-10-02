// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Fabrics;
using Metalama.Framework.Tests.ExtensionPoints;
using System;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_StaticMethod_Fabric;

// The calls to Console.WriteLine( string ) in the types selected by a project fabric are redirected to a static method. The call in a type that
// the fabric does not select is not redirected.

internal class Fabric : ProjectFabric
{
    public override void AmendProject( IProjectAmender amender )
        => amender.SelectTypes()
            .Where( t => t.Name == "Program" )
            .TestRedirectCalls(
                "WriteLine",
                "Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_StaticMethod_Fabric.Interceptors",
                "Log" );
}

internal static class Interceptors
{
    public static void Log( string s ) => Console.Out.WriteLine( $"intercepted: {s}" );
}

internal static class Other
{
    public static void M() => Console.WriteLine( "not intercepted" );
}

// <target>
internal static class Program
{
    public static void TestMain()
    {
        Console.WriteLine( "x" );
        Other.M();
    }
}
