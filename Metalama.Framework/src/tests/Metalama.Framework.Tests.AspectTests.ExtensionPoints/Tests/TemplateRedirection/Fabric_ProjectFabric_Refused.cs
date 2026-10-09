// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Fabrics;
using Metalama.Framework.Tests.ExtensionPoints;
using System;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Fabric_ProjectFabric_Refused;

// A project fabric requests the redirection of the calls in the type Program to a method declared from a template of the fabric. A project
// fabric cannot provide templates, so the declaration is refused with an explanation, and the call site is not redirected.

internal class Fabric : ProjectFabric
{
    public override void AmendProject( IProjectAmender amender )
        => amender.SelectTypes().Where( t => t.Name == "Program" ).TestRedirectCallsToTemplate( "Compute", nameof(this.Intercept) );

    [Template]
    public dynamic? Intercept()
    {
        Console.WriteLine( $"Intercepting {meta.Target.Method.Name}." );

        return meta.Proceed();
    }
}

internal static class Source
{
    public static int Compute( int x ) => x * 2;
}

// <target>
internal class Program
{
    public static int Execute() => Source.Compute( 21 );
}
