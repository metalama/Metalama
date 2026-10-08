// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.ExtensionPoints;
using System;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Template_PositionalBinding_SkipsNameBound;

// The template parameter 'b' binds by name to the parameter 'b' of the declared method. The template parameter 'q' has no name match, so it
// binds by position to the first parameter that no template parameter binds by name, which is 'a', and not to 'b'.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder ) => builder.TestRedirectCallsToTemplate( "Subtract", nameof(this.Intercept) );

    [Template]
    public dynamic? Intercept( int b, int q )
    {
        Console.WriteLine( $"q = {q}, b = {b}" );

        return meta.Proceed();
    }
}

internal static class Source
{
    public static int Subtract( int a, int b ) => a - b;
}

// <target>
[Redirect]
internal class Program
{
    public static int Execute() => Source.Subtract( 5, 3 );
}
