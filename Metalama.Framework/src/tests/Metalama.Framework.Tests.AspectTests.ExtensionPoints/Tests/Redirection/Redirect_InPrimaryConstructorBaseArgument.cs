// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.ExtensionPoints;
using System;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_InPrimaryConstructorBaseArgument;

// A call in the argument of the base type of a primary constructor is redirected.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
        => builder.TestRedirectCalls(
            "Compute",
            ( (INamedType) TypeFactory.GetType( typeof(Interceptors) ) ).Methods.OfName( "Compute" ).Single() );
}

internal static class Source
{
    public static int Compute( int x ) => x + 1;
}

internal static class Interceptors
{
    public static int Compute( int x ) => x * 10;
}

internal class Base
{
    public Base( int value )
    {
        this.Value = value;
    }

    public int Value { get; }
}

// <target>
[Redirect]
internal class Derived( int x ) : Base( Source.Compute( x ) ) { }

internal static class Program
{
    public static void TestMain() => Console.WriteLine( new Derived( 1 ).Value );
}
