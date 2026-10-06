// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.ExtensionPoints;
using System;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.MethodReferences.MethodGroup_Generic_ExplicitTypeArguments;

// A method group of a generic method is redirected with its type arguments written explicitly.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
        => builder.TestRedirectCalls(
            "Create",
            TypeFactory.GetNamedType( typeof(Interceptors) ).Methods.OfName( "Create" ).Single(),
            new TestRedirectionOptions { MethodReferences = true, ExplicitTypeArguments = true } );
}

internal static class Factory
{
    public static T? Create<T>() => default;
}

internal static class Interceptors
{
    public static T? Create<T>() => default;
}

// <target>
[Redirect]
internal static class Program
{
    public static void TestMain()
    {
        Func<string?> function = Factory.Create<string>;
        Console.WriteLine( function() == null );
    }
}
