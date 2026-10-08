// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.ExtensionPoints;
using System;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Static_IntroducedType;

// The method is declared in a class that the aspect introduces, which has no source code. The local variable 'x' of the template is renamed,
// because the lexical scope of a type without syntax includes the parameters of the declared method.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceClass( "Helpers" );

        builder.TestRedirectCallsToTemplate(
            "Compute",
            nameof(this.Intercept),
            new TestTemplateRedirectionOptions { Placement = "Type:" + builder.Target.FullName + ".Helpers" } );
    }

    [Template]
    public dynamic? Intercept()
    {
        var x = meta.Proceed();
        Console.WriteLine( $"Computed {x} in {meta.Target.Type.Name}." );

        return x;
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
