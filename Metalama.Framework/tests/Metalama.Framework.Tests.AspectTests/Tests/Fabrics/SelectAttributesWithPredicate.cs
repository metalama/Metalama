// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;

namespace Metalama.Framework.Tests.AspectTests.Tests.Fabrics.SelectAttributesWithPredicate;

public class Fabric : ProjectFabric
{
    public override void AmendProject( IProjectAmender amender )
    {
        amender.SelectDeclarationsWithAttribute<Marker>( a => a.Value )
            .OfType<IMethod>()
            .AddAspectIfEligible( m => new TheAspect() );
    }
}

[RunTimeOrCompileTime]
public class Marker : Attribute
{
    public Marker( bool value )
    {
        this.Value = value;
    }

    public bool Value { get; }
}

public class TheAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( $"Marked!" );

        return meta.Proceed();
    }
}

// <target>
[Marker( true )]
public class C
{
    [Marker( false )]
    public void UnmarkedMethod() { }

    [Marker( true )]
    public void MarkedMethod() { }
}