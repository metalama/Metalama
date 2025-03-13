// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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