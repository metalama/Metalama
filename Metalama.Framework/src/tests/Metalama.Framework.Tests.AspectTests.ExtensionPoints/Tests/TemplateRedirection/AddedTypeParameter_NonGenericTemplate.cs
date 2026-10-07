// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @MainMethod(TestMain)
#endif

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.ExtensionPoints;
using System;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.AddedTypeParameter_NonGenericTemplate;

// The template is not generic. The extension adds the type parameter 'TArg' to the builder after the locked part of the signature, makes it the
// type of the parameter 'value', and passes the static type of the argument at each call site. The two call sites share the declared method,
// and the template reads the added type parameter from the code model.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
        => builder.TestRedirectCallsToTemplate(
            "Log",
            nameof(this.Intercept),
            new TestTemplateRedirectionOptions { Placement = "Caller", PrebuiltBuilder = true, AddTypeParameterFor = "value" } );

    [Template]
    public void Intercept()
    {
        var typeArgument = meta.Target.Method.TypeParameters[0];
        Console.WriteLine( $"Logging a value of type {typeArgument.ToTypeOfExpression().Value!.Name}." );
        meta.Proceed();
    }
}

internal static class Source
{
    public static void Log( object value ) => Console.WriteLine( value );
}

// <target>
[Redirect]
internal class Program
{
    public static void TestMain()
    {
        Source.Log( 42 );
        Source.Log( "text" );
    }
}
