// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Fabrics;

namespace Metalama.Framework.Tests.AspectTests.Aspects.RecordAspectOrdering2;

// Verify that adding the same attribute to record and implicitly delcared methods on that record doesn't break.

internal class Fabric : ProjectFabric
{
    public override void AmendProject( IProjectAmender amender )
    {
        amender
            .SelectMany( compilation => compilation.AllTypes )
            .AddAspectIfEligible<LogAttribute>();

        amender
            .SelectMany( compilation => compilation.AllTypes )
            .SelectMany( type => type.Methods )
            .Where( method => method.Accessibility == Accessibility.Public && method.Name != "ToString" )
            .AddAspectIfEligible<LogAttribute>();
    }
}

public class LogAttribute : Aspect, IAspect<IDeclaration>
{
    public void BuildAspect( IAspectBuilder<IDeclaration> builder ) { }

    public void BuildEligibility( IEligibilityBuilder<IDeclaration> builder ) { }
}

// <target>
public record Person( string Name )
{
    public Guid Id { get; init; }
}