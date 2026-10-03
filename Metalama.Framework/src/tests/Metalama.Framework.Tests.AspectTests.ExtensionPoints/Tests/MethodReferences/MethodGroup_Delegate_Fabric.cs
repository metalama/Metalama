// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Fabrics;
using Metalama.Framework.Tests.ExtensionPoints;
using System;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.MethodReferences.MethodGroup_Delegate_Fabric;

// A project fabric redirects a method group converted to a delegate.

internal class Fabric : ProjectFabric
{
    public override void AmendProject( IProjectAmender amender )
        => amender.SelectTypes()
            .Where( t => t.Name == "Program" )
            .TestRedirectCalls(
                "Compute",
                "Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.MethodReferences.MethodGroup_Delegate_Fabric.Interceptors",
                "Compute",
                new TestRedirectionOptions { MethodReferences = true } );
}

internal static class Source
{
    public static int Compute( int x ) => x + 1;
}

internal static class Interceptors
{
    public static int Compute( int x ) => x * 10;
}

// <target>
internal static class Program
{
    public static void TestMain()
    {
        Func<int, int> converted = Source.Compute;
        Console.WriteLine( converted( 1 ) );
    }
}
