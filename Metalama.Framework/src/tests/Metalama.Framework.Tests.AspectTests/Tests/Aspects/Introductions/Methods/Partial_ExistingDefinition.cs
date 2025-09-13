// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @Skipped(#35847)
#endif

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.Partial_ExistingDefinition;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceMethod( nameof(PartialMethod) );
    }

    [Template(IsPartial = true)]
    private void PartialMethod()
    {
        Console.WriteLine("Implementation");
    }
}

// <target>
[Introduction]
internal partial class TargetClass
{
#if TESTRUNNER
    partial void PartialMethod();
#endif
}