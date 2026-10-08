// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.ExtensionPoints;
using System;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Instance_BaseType;

// The calls in a derived type are redirected to a protected instance method declared in its base type, which is called on 'this'.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
        => builder.TestRedirectCallsToTemplate( "Add", nameof(this.Intercept), new TestTemplateRedirectionOptions { Placement = "Type:Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Instance_BaseType.Base", IsInstance = true, Accessibility = "Protected" } );

    [Template]
    public dynamic? Intercept() => meta.Proceed();
}

internal class Bag
{
    public void Add( int value ) => Console.WriteLine( $"Added {value}." );
}

// <target>
internal class Base { }

// <target>
[Redirect]
internal class Derived : Base
{
    private readonly Bag _bag = new();

    public void Fill() => this._bag.Add( 1 );
}
