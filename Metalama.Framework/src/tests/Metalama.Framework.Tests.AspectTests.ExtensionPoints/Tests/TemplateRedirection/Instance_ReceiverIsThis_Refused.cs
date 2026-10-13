// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.ExtensionPoints;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Instance_ReceiverIsThis_Refused;

// The calls are redirected to an instance method of the calling type, which is called on the receiver of each call site. The call whose
// receiver is of a base type of the calling type is refused, because the method cannot be called on it. The base call is refused, because a call
// of the method on 'base' would not find it. The call on 'this' is redirected.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
        => builder.TestRedirectCallsToTemplate(
            "Increment",
            nameof(this.Intercept),
            new TestTemplateRedirectionOptions { Placement = "Caller", IsInstance = true, Accessibility = "Private", ReceiverIsThis = true } );

    [Template]
    public dynamic? Intercept() => meta.Proceed();
}

internal class Counter
{
    public void Increment( int amount ) { }
}

// <target>
[Redirect]
internal class Program : Counter
{
    public void Run( Counter other )
    {
        this.Increment( 1 );
        other.Increment( 2 );
        base.Increment( 3 );
    }
}
