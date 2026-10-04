// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.ExtensionPoints;
using System;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_PromotedFieldInitializers_WarningOrder;

// The aspect overrides two fields, which promotes them to properties, and redirects the calls in their initializers. The linker cannot apply
// either redirection, and it reports one warning per redirection, in the order in which the redirections were requested.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach ( var field in builder.Target.Fields )
        {
            builder.With( field ).Override( nameof(this.Template) );
        }

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
    private int _second = Source.Compute( 2 );
    private int _first = Source.Compute( 1 );

    public static void TestMain() => Console.WriteLine( new Program()._first + new Program()._second );
}
