// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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