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

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Instance_CallingType_This;

// The calls are redirected to an instance method declared in the calling type, which is called on 'this' and receives the receiver of the
// call as its first parameter. The call site in the static method is refused, because 'this' is not available there.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
        => builder.TestRedirectCallsToTemplate(
            "Add",
            nameof(this.Intercept),
            new TestTemplateRedirectionOptions { Placement = "Caller", IsInstance = true, Accessibility = "Private" } );

    [Template]
    public dynamic? Intercept()
    {
        Console.WriteLine( $"Adding to the bag of {meta.This.Name}." );

        return meta.Proceed();
    }
}

internal class Bag
{
    public void Add( int value ) => Console.WriteLine( $"Added {value}." );
}

// <target>
[Redirect]
internal class Program
{
    public string Name { get; } = "the program";

    private readonly Bag _bag = new();

    public void Fill()
    {
        this._bag.Add( 1 );
        Action add = () => this._bag.Add( 2 );
        add();
    }

    public static void TestMain()
    {
        new Program().Fill();
        new Bag().Add( 3 );
    }
}
