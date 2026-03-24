// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @IncludeAllSeverities
#endif

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug780;

[assembly: AspectOrder( AspectOrderDirection.CompileTime, typeof(OverridePartialAspect), typeof(CheckHasImplementationAspect) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug780;

/*
 * Tests that HasImplementation changes to true after a partial method without implementation is overridden by an aspect (#780).
 */

public class OverridePartialAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach (var method in builder.Target.Methods)
        {
            builder.With( method ).Override( nameof(Template) );
        }
    }

    [Template]
    public dynamic? Template()
    {
        Console.WriteLine( "Overridden." );

        return meta.Proceed();
    }
}

public class CheckHasImplementationAspect : TypeAspect
{
    private static readonly DiagnosticDefinition<(string MethodName, bool HasImplementation)> _hasImplementationResult = new(
        "BUG780",
        Severity.Warning,
        "Method '{0}' HasImplementation: {1}" );

    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach (var method in builder.Target.Methods)
        {
            builder.Diagnostics.Report(
                _hasImplementationResult.WithArguments( (method.Name, method.HasImplementation) ) );
        }
    }
}

// <target>
[OverridePartialAspect]
[CheckHasImplementationAspect]
internal partial class TargetClass
{
    partial void PartialWithoutImpl();

    partial void PartialWithImpl();
}

// <target>
internal partial class TargetClass
{
    partial void PartialWithImpl()
    {
        Console.WriteLine( "Original implementation." );
    }
}
