// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.ExtensionPoints;
using System;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Static_StaticClass_Proceed;

// The calls to a static method are redirected to a method that the extension declares in a static class, and whose body is generated from a
// template. Both call sites share the declared method, and meta.Proceed() invokes the source method.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder ) => builder.TestRedirectCallsToTemplate( "Compute", nameof(this.Intercept) );

    [Template]
    public dynamic? Intercept()
    {
        Console.WriteLine( $"Intercepting {meta.Target.Method.Name} with {meta.Target.Parameters.Count} parameter(s)." );

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
        Console.WriteLine( Source.Compute( 21 ) );
        Console.WriteLine( Source.Compute( 1 ) );
    }
}
