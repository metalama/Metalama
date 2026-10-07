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

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Void_RefArgument;

// The source method returns void and has a ref parameter. The declared method passes the parameter by reference to the source method.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
        => builder.TestRedirectCallsToTemplate( "Increment", nameof(this.Intercept), new TestTemplateRedirectionOptions { Placement = "Caller" } );

    [Template]
    public void Intercept()
    {
        Console.WriteLine( "Incrementing." );
        meta.Proceed();
    }
}

internal static class Source
{
    public static void Increment( ref int value ) => value++;
}

// <target>
[Redirect]
internal class Program
{
    public static void TestMain()
    {
        var value = 1;
        Source.Increment( ref value );
        Console.WriteLine( value );
    }
}
