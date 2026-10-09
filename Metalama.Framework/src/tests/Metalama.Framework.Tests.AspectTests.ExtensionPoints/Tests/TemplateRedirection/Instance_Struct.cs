// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.ExtensionPoints;
using System;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Instance_Struct;

// The calls in a struct are redirected to an instance method of the struct, which is called on 'this'.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
        => builder.TestRedirectCallsToTemplate( "Add", nameof(this.Intercept), new TestTemplateRedirectionOptions { Placement = "Caller", IsInstance = true, Accessibility = "Private" } );

    [Template]
    public dynamic? Intercept() => meta.Proceed();
}

internal class Bag
{
    public void Add( int value ) => Console.WriteLine( $"Added {value}." );
}

// <target>
[Redirect]
internal struct Filler
{
    private readonly Bag _bag;

    public Filler( Bag bag )
    {
        this._bag = bag;
    }

    public void Fill() => this._bag.Add( 1 );
}
