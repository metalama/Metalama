// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Linq;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;
using Metalama.Framework.Serialization;

namespace Metalama.Framework.Tests.AspectTests.Tests.Fabrics.SelectAttributes_Inheritable;

public class Fabric : ProjectFabric
{
    public override void AmendProject( IProjectAmender amender )
    {
        amender.SelectDeclarationsWithAttribute<Marker>()
            .OfType<INamedType>()
            .AddAspectIfEligible(
                m =>
                {
                    var attribute = m.Attributes.GetConstructedAttributesOfType<Marker>().Single();

                    return new TheAspect( attribute );
                } );
    }
}

[RunTimeOrCompileTime]
public class Marker : Attribute, ICompileTimeSerializable
{
    public Marker( string value )
    {
        this.Value = value;
    }

    public string Value { get; }
}

[Inheritable]
public class TheAspect : TypeAspect
{
    private readonly Marker _marker;

    public TheAspect( Marker marker )
    {
        this._marker = marker;
    }

    [Introduce( WhenExists = OverrideStrategy.Override )]
    public virtual void Introduced()
    {
        Console.WriteLine( $"Marker: {this._marker.Value}" );
    }
}

[Marker( "The Marker" )]
public class BaseClass { }