// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.ExtensionPoints;
using System;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Restrictions_AddConstraintToLockedTypeParameter;

// The type parameter of the declared method is copied from the source method and locked. Constraints can be added to a locked type
// parameter, so the declared method has the struct and IComparable constraints.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
        => builder.TestRedirectCallsToTemplate(
            "Echo",
            nameof(this.Intercept),
            new TestTemplateRedirectionOptions { Placement = "Caller", Generic = true, PrebuiltBuilder = true, AddConstraintToLockedTypeParameter = "T" } );

    [Template]
    public T Intercept<T>( T value ) => meta.Proceed()!;
}

internal static class Source
{
    public static T Echo<T>( T value ) => value;
}

// <target>
[Redirect]
internal class Program
{
    public static void TestMain() => Console.WriteLine( Source.Echo( 1 ) );
}
