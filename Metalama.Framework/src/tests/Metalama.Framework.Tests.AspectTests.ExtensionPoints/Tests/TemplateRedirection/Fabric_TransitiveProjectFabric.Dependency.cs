// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Fabrics;
using Metalama.Framework.Tests.ExtensionPoints;
using System;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Fabric_TransitiveProjectFabric;

public class TransitiveFabric : TransitiveProjectFabric
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
