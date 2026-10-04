// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.ExtensionPoints;
using System;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_InReceiver;

// A call to a method of a read-only struct is redirected to a static method that receives the receiver with the in modifier.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
        => builder.TestRedirectCalls(
            "LengthSquared",
            TypeFactory.GetNamedType( typeof(Interceptors) ).Methods.OfName( "LengthSquared" ).Single(),
            new TestRedirectionOptions { ReceiverMode = "FirstArgumentByIn" } );
}

internal readonly struct Vector
{
    public Vector( int x, int y )
    {
        this.X = x;
        this.Y = y;
    }

    public int X { get; }

    public int Y { get; }

    public int LengthSquared() => (this.X * this.X) + (this.Y * this.Y);
}

internal static class Interceptors
{
    public static int LengthSquared( in Vector vector ) => -1;
}

// <target>
[Redirect]
internal static class Program
{
    public static void TestMain()
    {
        var vector = new Vector( 3, 4 );
        Console.WriteLine( vector.LengthSquared() );
    }
}
