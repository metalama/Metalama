// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Fabrics;

namespace Metalama.Framework.Tests.AspectTests.Aspects.RecordAspectOrdering;

internal class Fabric : ProjectFabric
{
    public override void AmendProject( IProjectAmender amender )
        => amender
            .SelectMany( compilation => compilation.AllTypes )
            .SelectMany( type => type.Methods )
            .AddAspectIfEligible<LogAttribute>();
}

public class LogAttribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( $"{meta.Target.Method} started." );

        return meta.Proceed();
    }

    public override void BuildEligibility( IEligibilityBuilder<IMethod> builder )
    {
        // Don't call base to skip MustBeExplicitlyDeclared rule.
    }
}

// <target>
public record Person( string Name )
{
    public Guid Id { get; init; }
}