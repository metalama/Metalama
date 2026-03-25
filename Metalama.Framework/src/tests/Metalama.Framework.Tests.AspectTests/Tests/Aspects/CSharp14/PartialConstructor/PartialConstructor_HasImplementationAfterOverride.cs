// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_5_0_0_OR_GREATER)
// @IncludeAllSeverities
#endif

#if ROSLYN_5_0_0_OR_GREATER

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;

[assembly: AspectOrder( AspectOrderDirection.CompileTime,
    typeof(Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.PartialConstructor_HasImplementationAfterOverride.OverrideAspect),
    typeof(Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.PartialConstructor_HasImplementationAfterOverride.CheckAspect) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.PartialConstructor_HasImplementationAfterOverride;

/*
 * Tests that IConstructor.HasImplementation changes to true after a partial constructor
 * without implementation is overridden by an aspect (#780).
 */

public class OverrideAspect : ConstructorAspect
{
    public override void BuildAspect( IAspectBuilder<IConstructor> builder )
    {
        builder.Override( nameof( Template ) );
    }

    [Template]
    public void Template()
    {
        Console.WriteLine( "Overridden." );
    }
}

public class CheckAspect : TypeAspect
{
    private static readonly DiagnosticDefinition<(string Name, bool HasImplementation)> _diagnostic = new(
        "BUG780C",
        Severity.Warning,
        "Constructor '{0}' HasImplementation: {1}" );

    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach ( var ctor in builder.Target.Constructors )
        {
            builder.Diagnostics.Report(
                _diagnostic.WithArguments( (ctor.ToDisplayString(), ctor.HasImplementation) ) );
        }
    }
}

// <target>
[CheckAspect]
internal partial class C
{
#if TESTRUNNER
    [OverrideAspect]
    public partial C();
#endif
}

#endif
