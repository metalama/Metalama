// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Linq;
using Metalama.Framework.Fabrics;
using Metalama.Framework.Options;
#if TEST_OPTIONS
// @Include(_Common.cs)
#endif

#pragma warning disable CS0067

namespace Metalama.Framework.Tests.AspectTests.Tests.Options.AspectModifyingProjectFabric;

public class Fabric : ProjectFabric
{
    public override void AmendProject( IProjectAmender amender )
    {
        amender.SetOptions( c => new MyOptions { Value = "FromFabric.Project" } );
        amender.Select( c => c.Types.OfName( nameof(C2) ).Single() ).SetOptions( c => new MyOptions { Value = "FromFabric.C2" } );

        amender.Select( c => c.Types.OfName( nameof(C2) ).Single() )
            .Select( t => t.Methods.OfName( nameof(C2.M2) ).Single() )
            .SetOptions( c => new MyOptions { Value = "FromFabric.M2" } );
    }
}

// <target>
[ShowOptionsAspect]
[ModifyOptionsAspect( "FromAspect.C1" )]
public class C1
{
    [ShowOptionsAspect]
    public void M( [ShowOptionsAspect] int p ) { }
}

// <target>
[ShowOptionsAspect]
[ModifyOptionsAspect( "FromAspect.C2" )]
public class C2
{
    [ShowOptionsAspect]
    public void M( [ShowOptionsAspect] int p ) { }

    [ShowOptionsAspect]
    [ModifyOptionsAspect( "FromAspect.P" )]
    public void M2( [ShowOptionsAspect] int p ) { }

    [ShowOptionsAspect]
    public int P
    {
        [ShowOptionsAspect]
        get;
        [ShowOptionsAspect]
        set;
    }

    [ShowOptionsAspect]
    public int F;

    [ShowOptionsAspect]
    public event EventHandler? E;

    public class N
    {
        [ShowOptionsAspect]
        public void M( [ShowOptionsAspect] int p ) { }
    }
}