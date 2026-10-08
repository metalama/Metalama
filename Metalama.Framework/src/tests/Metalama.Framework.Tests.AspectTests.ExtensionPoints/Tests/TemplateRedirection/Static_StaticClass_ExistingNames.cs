// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.ExtensionPoints;
using System;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Static_StaticClass_ExistingNames;

// The calls of Compute are redirected to a static class whose full name is the full name of the existing class Source. The calls of Twice
// are redirected to a static class in the namespace Generated, which does not exist under the namespace of the test, which exists.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.TestRedirectCallsToTemplate( "Compute", nameof(this.Intercept), new TestTemplateRedirectionOptions { StaticClassName = "Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Static_StaticClass_ExistingNames.Source" } );
        builder.TestRedirectCallsToTemplate( "Twice", nameof(this.Intercept), new TestTemplateRedirectionOptions { StaticClassName = "Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Static_StaticClass_ExistingNames.Generated.Interceptors" } );
    }

    [Template]
    public dynamic? Intercept() => meta.Proceed();
}

internal static class Source
{
    public static int Compute( int x ) => x * 2;

    public static int Twice( int x ) => x * 2;
}

// <target>
[Redirect]
internal class Program
{
    public static int Execute() => Source.Compute( 21 ) + Source.Twice( 1 );
}
