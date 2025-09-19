// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @TestScenario(DesignTime)
#endif

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

#pragma warning disable IDE0005 // We weirdly have implicit global usings but they don't appear in the inpuc csc files
using System;
#pragma warning restore IDE0005

namespace Metalama.Framework.IntegrationTests.Aspects.DesignTime.IntroduceMethod_Partial_ExistingImplementation;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceMethod( nameof(PartialMethod) );
    }

    [Template(IsPartial = true)]
    private extern void PartialMethod();
}

// <target>
[Introduction]
internal partial class TargetClass 
{
#if TESTRUNNER
    partial void PartialMethod()
    {
        Console.WriteLine("Implementation");
    }
#endif
}