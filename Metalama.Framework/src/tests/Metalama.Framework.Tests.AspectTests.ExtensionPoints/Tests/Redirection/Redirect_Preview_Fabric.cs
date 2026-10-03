// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @TestScenario(Preview)
#endif

using Metalama.Framework.Fabrics;
using Metalama.Framework.Tests.ExtensionPoints;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_Preview_Fabric;

// The transforming hook runs in the preview scenario, also for a redirection that a project fabric requests, so the preview of a file shows
// the redirected call.

internal class Fabric : ProjectFabric
{
    public override void AmendProject( IProjectAmender amender )
        => amender.SelectTypes()
            .Where( t => t.Name == "C" )
            .TestRedirectCalls(
                "Compute",
                "Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_Preview_Fabric.Interceptors",
                "Compute" );
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
internal class C
{
    private int M() => Source.Compute( 1 );
}
