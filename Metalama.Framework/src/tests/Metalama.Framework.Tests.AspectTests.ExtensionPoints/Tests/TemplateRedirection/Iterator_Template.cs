// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.ExtensionPoints;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Iterator_Template;

// The template is an iterator, so the declared method is an iterator that enumerates the result of the intercepted method.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
        => builder.TestRedirectCallsToTemplate( "Range", nameof(this.Intercept), new TestTemplateRedirectionOptions {  } );

    [Template]
    public IEnumerable<int> Intercept()
    {
        foreach ( var item in (IEnumerable<int>) meta.Proceed()! )
        {
            Console.WriteLine( $"Yielding {item}." );

            yield return item;
        }
    }
}

internal static class Source
{
    public static IEnumerable<int> Range( int count )
    {
        for ( var i = 0; i < count; i++ )
        {
            yield return i;
        }
    }
}

// <target>
[Redirect]
internal class Program
{
    public static IEnumerable<int> Execute() => Source.Range( 2 );
}
