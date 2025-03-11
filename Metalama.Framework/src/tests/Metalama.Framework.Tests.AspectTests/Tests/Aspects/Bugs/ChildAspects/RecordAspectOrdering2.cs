// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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