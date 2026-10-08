// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.ExtensionPoints;
using System;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.TemplateError_LocatedAtSite;

// The template throws an exception during its expansion. The error is located at the call site for which the method was declared, and the call
// site is not redirected.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder ) => builder.TestRedirectCallsToTemplate( "Compute", nameof(this.Intercept) );

    [Template]
    public dynamic? Intercept()
    {
        var length = Refuse( meta.Target.Method.Name );

        return meta.Proceed();
    }

    private static int Refuse( string name ) => throw new InvalidOperationException( $"The template refuses '{name}'." );
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
