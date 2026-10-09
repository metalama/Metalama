// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.ExtensionPoints;
using System;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Restrictions_ReassignEqualReturnType;

// The return type of the pre-built builder is locked, and it is assigned a new instance of the same type. An equal type is not a change, so
// the restrictions accept it and the call site is redirected.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
        => builder.TestRedirectCallsToTemplate(
            "Compute",
            nameof(this.Intercept),
            new TestTemplateRedirectionOptions { PrebuiltBuilder = true, ReassignReturnType = true } );

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
    public static int Execute() => Source.Compute( 21 );
}
