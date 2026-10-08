// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.ExtensionPoints;
using System;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.MetaExtension_Missing_Error;

// The template reads a meta extension that the declared method does not have, so the expansion of the template reports an error.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
        => builder.TestRedirectCallsToTemplate( "Compute", nameof(this.Intercept), new TestTemplateRedirectionOptions {  } );

    [Template]
    public dynamic? Intercept()
    {
        Console.WriteLine( meta.GetExtension<TestMetaExtension>().Description );

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
    public static int Execute() => Source.Compute( 21 );
}
