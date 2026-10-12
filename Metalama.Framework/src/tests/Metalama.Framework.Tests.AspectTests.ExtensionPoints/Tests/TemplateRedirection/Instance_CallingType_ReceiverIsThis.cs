// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.ExtensionPoints;
using System;

// The test is about a call whose receiver is the implicit this.
#pragma warning disable IDE0009

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Instance_CallingType_ReceiverIsThis;

// The calls are redirected to an instance method declared in the calling type, whose receiver parameter is removed from the pre-built builder.
// The call sites, whose receiver is an implicit or explicit 'this', drop their receiver, and meta.Proceed() calls the protected method of the
// base type on 'this'.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
        => builder.TestRedirectCallsToTemplate(
            "Update",
            nameof(this.Intercept),
            new TestTemplateRedirectionOptions
            {
                Placement = "Caller", IsInstance = true, Accessibility = "Private", PrebuiltBuilder = true, ReceiverIsThis = true
            } );

    [Template]
    public dynamic? Intercept()
    {
        Console.WriteLine( "Updating." );

        return meta.Proceed();
    }
}

internal class Control
{
    protected void Update( int value ) => Console.WriteLine( $"{this.GetType().Name} updated with {value}." );
}

// <target>
[Redirect]
internal class Grid : Control
{
    public void Refresh()
    {
        this.Update( 1 );
        Update( 2 );
    }

    public static void TestMain() => new Grid().Refresh();
}
