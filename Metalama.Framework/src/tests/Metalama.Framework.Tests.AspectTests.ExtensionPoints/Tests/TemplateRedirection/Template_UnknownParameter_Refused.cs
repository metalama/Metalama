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

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Template_UnknownParameter_Refused;

// The template has a run-time parameter that matches no parameter of the declared method, so the declaration is refused and the call site
// is not redirected.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder ) => builder.TestRedirectCallsToTemplate( "Compute", nameof(this.Intercept) );

    [Template]
    public dynamic? Intercept( int x, int y )
    {
        return meta.Proceed();
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
    public static void TestMain() => Console.WriteLine( Source.Compute( 21 ) );
}
