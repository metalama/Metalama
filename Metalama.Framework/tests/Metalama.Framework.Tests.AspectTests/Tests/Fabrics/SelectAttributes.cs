// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Linq;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;

namespace Metalama.Framework.Tests.AspectTests.Tests.Fabrics.SelectAttributes;

public class Fabric : ProjectFabric
{
    public override void AmendProject( IProjectAmender amender )
    {
        amender.SelectDeclarationsWithAttribute<Marker>()
            .OfType<IMethod>()
            .AddAspectIfEligible(
                m =>
                {
                    var attribute = m.Attributes.GetConstructedAttributesOfType<Marker>().Single();

                    return new TheAspect( attribute.Value );
                } );
    }
}

[RunTimeOrCompileTime]
public class Marker : Attribute
{
    public string? Value { get; set; }
}

public class DerivedMarker : Marker { }

public class TheAspect : OverrideMethodAspect
{
    private readonly string? _marker;

    public TheAspect( string? marker )
    {
        this._marker = marker;
    }

    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( $"Marker: {this._marker}" );

        return meta.Proceed();
    }
}

// <target>
[Marker]
public class C
{
    public void UnmarkedMethod() { }

    [Marker( Value = "TheMarker" )]
    public void MarkedMethod() { }

    [DerivedMarker( Value = "DerivedMarker" )]
    public void DerivedMethod() { }
}