// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Tests.ExtensionPoints;
using System;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_InFieldInitializer_SplitDeclarators;

// The aspect adds an attribute to one of two fields declared in the same declaration, so the linker splits the declaration. The calls in both
// initializers are redirected.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.With( builder.Target.Fields.OfName( "_a" ).Single() ).IntroduceAttribute( AttributeConstruction.Create( typeof(ObsoleteAttribute) ) );

        builder.TestRedirectCalls(
            "Compute",
            TypeFactory.GetNamedType( typeof(Interceptors) ).Methods.OfName( "Compute" ).Single() );
    }
}

internal static class Source
{
    public static int Compute( int x ) => x + 1;
}

internal static class Interceptors
{
    public static int Compute( int x ) => x * 10;
}

// <target>
[Redirect]
internal class Program
{
    private int _a = Source.Compute( 1 ), _b = Source.Compute( 2 );

#pragma warning disable CS0612
    public static void TestMain() => Console.WriteLine( new Program()._a + new Program()._b );
#pragma warning restore CS0612
}
