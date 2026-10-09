// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.ExtensionPoints;
using System;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Template_DefaultValueParameter;

// The template has run-time parameters with default values that match no parameter of the declared method. The template receives their
// default values: a literal, a literal with a type suffix, null, a value of an enumeration and the default value of a structure.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder ) => builder.TestRedirectCallsToTemplate( "Compute", nameof(this.Intercept) );

    [Template]
    public dynamic? Intercept(
        int x,
        int factor = 3,
        long offset = 5,
        string? label = null,
        DayOfWeek day = DayOfWeek.Monday,
        DateTime when = default )
    {
        Console.WriteLine( $"{label} {factor} {offset} {day} {when.Ticks}" );

        return meta.Proceed();
    }
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
