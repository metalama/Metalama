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

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.ConditionalAccess_Forwarder;

// A call in a conditional access is redirected through a forwarder of the declared method, so that a null receiver short-circuits the call
// and the template does not run.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder ) => builder.TestRedirectCallsToTemplate( "Length", nameof(this.Intercept) );

    [Template]
    public dynamic? Intercept()
    {
        Console.WriteLine( "Intercepting Length." );

        return meta.Proceed();
    }
}

internal class Text
{
    public string Value { get; set; } = "abc";

    public int Length() => this.Value.Length;
}

// <target>
[Redirect]
internal class Program
{
    public static void TestMain()
    {
        var text = (Text?) new Text();
        Console.WriteLine( text?.Length() );
        text = null;
        Console.WriteLine( text?.Length() ?? -1 );
    }
}
