// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.ExtensionPoints;
using System;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Instance_Initializers_Refused;

// The calls are redirected to an instance method of the calling type. The calls in the initializer of a property and of an event field are
// refused, because 'this' is not available there. The call in a method is redirected.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
        => builder.TestRedirectCallsToTemplate( "Measure", nameof(this.Intercept), new TestTemplateRedirectionOptions { Placement = "Caller", IsInstance = true, Accessibility = "Private" } );

    [Template]
    public dynamic? Intercept() => meta.Proceed();
}

internal static class Source
{
    public static int Measure( string text ) => text.Length;

    public static EventHandler? Measure( int value ) => null;
}

// <target>
[Redirect]
internal class Program
{
    public int Length { get; } = Source.Measure( "property" );

    public event EventHandler? Changed = Source.Measure( 1 );

    public int Compute() => Source.Measure( "method" );
}
