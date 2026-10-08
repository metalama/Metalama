// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.ExtensionPoints;
using System;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.ProceedBinding_Invalid_Refused;

// The proceed binding gives a parameter index that the declared method does not have. The declaration is refused when the method is declared,
// and the call site is not redirected.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
        => builder.TestRedirectCallsToTemplate( "Compute", nameof(this.Intercept), new TestTemplateRedirectionOptions { InvalidProceedBinding = true } );

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
