// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @IncludeLineNumberInDiagnosticReport
#endif

using System;
using System.Linq;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Constructors.NonInlineable_IntroducedMember;

/*
 * Tests that non-inlineable constructor override diagnostic is reported with the correct location
 * when the aspect also introduces members (which modifies the intermediate compilation's syntax tree).
 * Regression test for #818: linker diagnostics should reference the source compilation, not the intermediate compilation.
 */

public class OverrideAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.With( builder.Target.Constructors.Single() ).Override( nameof(Template) );
        builder.IntroduceMethod( nameof(IntroducedMethod) );
    }

    [Template]
    public void Template()
    {
        Console.WriteLine( "This is the override." );

        meta.Proceed();
        meta.Proceed();
    }

    [Template]
    public void IntroducedMethod()
    {
        Console.WriteLine( "This is an introduced method." );
    }
}

// <target>
[Override]
internal class TargetClass
{
    public TargetClass()
    {
        Console.WriteLine( $"This is the original constructor." );
    }
}
