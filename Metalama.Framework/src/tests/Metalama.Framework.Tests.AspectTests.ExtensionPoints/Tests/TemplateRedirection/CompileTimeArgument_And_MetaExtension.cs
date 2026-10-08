// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.ExtensionPoints;
using System;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.CompileTimeArgument_And_MetaExtension;

// The template receives a compile-time argument and reads the meta extension that the test extension exposes. Each call site gets its own
// declared method, whose names receive numeric suffixes.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
        => builder.TestRedirectCallsToTemplate(
            "Compute",
            nameof(this.Intercept),
            new TestTemplateRedirectionOptions { Label = "[log]", WithMetaExtension = true, OneMethodPerSite = true } );

    [Template]
    public dynamic? Intercept( [CompileTime] string label )
    {
        Console.WriteLine( label + " " + meta.GetExtension<TestMetaExtension>().Description );

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
    public static void TestMain()
    {
        Console.WriteLine( Source.Compute( 1 ) );
        Console.WriteLine( Source.Compute( 2 ) );
    }
}
