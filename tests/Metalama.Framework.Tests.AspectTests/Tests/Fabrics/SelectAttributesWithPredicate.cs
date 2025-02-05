// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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