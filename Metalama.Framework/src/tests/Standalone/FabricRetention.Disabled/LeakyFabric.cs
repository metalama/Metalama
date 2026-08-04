// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;
using System.Collections.Generic;

namespace FabricRetentionDisabled;

internal class MyAspect : TypeAspect { }

/// <summary>
/// The same fabric as in the <c>FabricRetention</c> scenario, so that the only difference between the two is the
/// MSBuild property.
/// </summary>
internal class LeakyFabric : ProjectFabric
{
    private readonly List<INamedType> _seen = new();

    public override void AmendProject( IProjectAmender amender )
    {
        amender.SelectTypes()
            .Where( t => { this._seen.Add( t ); return t.Name.StartsWith( "Target" ); } )
            .AddAspect<MyAspect>();
    }
}

internal class TargetClass { }
