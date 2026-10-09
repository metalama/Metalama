// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;
using Metalama.Framework.Tests.ExtensionPoints;
using System;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Fabric_TypeFabric;

// A type fabric redirects the calls in its type to a method declared from a template of the fabric. The origin of the declaration is the
// fabric, whose template class gives the template.

internal static class Source
{
    public static int Compute( int x ) => x * 2;
}

// <target>
internal class Program
{
    public static int Execute() => Source.Compute( 21 );

    private class Fabric : TypeFabric
    {
        public override void AmendType( ITypeAmender amender )
            => ((IAdviser<INamedType>) amender).TestRedirectCallsToTemplate( "Compute", nameof(this.Intercept) );

        [Template]
        public dynamic? Intercept()
        {
            Console.WriteLine( $"Intercepting {meta.Target.Method.Name}." );

            return meta.Proceed();
        }
    }
}
