// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @LanguageVersion(13)
#endif

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.ExtensionPoints;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_Arguments_PackedParamsCollection;

// The source method has a params collection of C# 13. Its expanded elements are packed into a collection expression, which creates the same
// collection as the compiler.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.TestRedirectCalls(
            "Sum",
            ( (INamedType) TypeFactory.GetType( typeof(Interceptors) ) ).Methods.OfName( "Sum" ).Single(),
            new TestRedirectionOptions { Arguments = "argument:1; argument:0" } );
    }
}

internal static class Source
{
    public static string Sum( string label, params List<int> values ) => $"{label} {values.Sum()}";
}

internal static class Interceptors
{
    public static string Sum( List<int> values, string label ) => $"intercepted {label} {values.Count} {values.Sum()}";
}

// <target>
[Redirect]
internal static class Program
{
    public static void TestMain()
    {
        Console.WriteLine( Source.Sum( "a", 1, 2 ) );
        Console.WriteLine( Source.Sum( "b" ) );
    }
}
