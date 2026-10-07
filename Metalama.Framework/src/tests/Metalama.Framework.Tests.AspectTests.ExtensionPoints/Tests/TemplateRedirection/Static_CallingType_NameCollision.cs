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

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Static_CallingType_NameCollision;

// The method is declared in the calling type, which already has a member named Compute_Interceptor, so the declared method receives a
// numeric suffix. The local variable 'x' of the template is renamed, because the declared method has a parameter 'x'.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
        => builder.TestRedirectCallsToTemplate(
            "Compute",
            nameof(this.Intercept),
            new TestTemplateRedirectionOptions { Placement = "Caller", Accessibility = "Private" } );

    [Template]
    public dynamic? Intercept()
    {
        var x = meta.Proceed();
        Console.WriteLine( $"Computed {x}." );

        return x;
    }
}

internal static class Source
{
    public static int Compute( int x ) => x * 2;
}

// <target>
[Redirect]
internal class Program
{
    public static int Compute_Interceptor = 0;

    public static void TestMain() => Console.WriteLine( Source.Compute( 21 ) + Compute_Interceptor );
}
