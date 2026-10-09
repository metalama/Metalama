// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.ExtensionPoints;
using System;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Restrictions_ChangeLockedRefKind_Refused;

// The signature is set on a builder created with CreateMethodBuilder, whose first parameter is locked. Changing its reference kind throws,
// so the method is not declared and the call site is left unchanged.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
        => builder.TestRedirectCallsToTemplate(
            "Compute",
            nameof(this.Intercept),
            new TestTemplateRedirectionOptions { Placement = "Caller", PrebuiltBuilder = true, ChangeLockedRefKind = true } );

    [Template]
    public dynamic? Intercept() => meta.Proceed();
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
