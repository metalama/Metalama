// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Fabric_TransitiveProjectFabric;

// A transitive project fabric of a referenced project redirects the calls in the type Program to a method declared from a template of the
// fabric. The fabric is the default template provider, so the template is resolved against the fabric class of the referenced project
// (issue #2236).

internal static class Source
{
    public static int Compute( int x ) => x * 2;
}

// <target>
internal class Program
{
    public static int Execute() => Source.Compute( 21 );
}
