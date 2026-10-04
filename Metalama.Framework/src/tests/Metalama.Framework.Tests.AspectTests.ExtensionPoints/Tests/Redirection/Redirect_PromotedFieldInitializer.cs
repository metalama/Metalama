// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.ExtensionPoints;
using System;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_PromotedFieldInitializer;

// The aspect overrides a field, which promotes it to a property, and redirects the call in its initializer. The linker does not find the
// call in the initializer of the promoted field, so it reports a warning and the call is not redirected.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.With( builder.Target.Fields.OfName( "_value" ).Single() ).Override( nameof(this.Template) );

        builder.TestRedirectCalls(
            "Compute",
            TypeFactory.GetNamedType( typeof(Interceptors) ).Methods.OfName( "Compute" ).Single() );
    }

    [Template]
    public dynamic? Template
    {
        get => meta.Proceed();
        set => meta.Proceed();
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
    private int _value = Source.Compute( 1 );

    public static void TestMain() => Console.WriteLine( new Program()._value );
}
