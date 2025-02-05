// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.Linq;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;

namespace Metalama.Framework.Tests.AspectTests.Tests.Fabrics.SelectAttributes_Inheritable_Ref;

public class Fabric : ProjectFabric
{
    public override void AmendProject( IProjectAmender amender )
    {
        amender.SelectDeclarationsWithAttribute( typeof(Marker) )
            .OfType<INamedType>()
            .AddAspectIfEligible(
                m =>
                {
                    var attribute = m.Attributes.OfAttributeType( typeof(Marker) ).Single();

                    return new TheAspect( attribute.ToRef() );
                } );
    }
}

public class Marker : Attribute
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
    private IRef<IAttribute> _attribute;

    public TheAspect( IRef<IAttribute> attribute )
    {
        this._attribute = attribute;
    }

    [Introduce( WhenExists = OverrideStrategy.Override )]
    public virtual void Introduced()
    {
        var attribute = this._attribute.GetTarget( meta.Target.Compilation );
        var value = (string) attribute.ConstructorArguments[0].Value!;

        Console.WriteLine( $"Marker: {value}" );
    }
}

[Marker( "The Marker" )]
public class BaseClass { }