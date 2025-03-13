// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.Partial_Mandatory;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceMethod( nameof(PublicMethod) );
        builder.IntroduceMethod( nameof(NonVoidMethod) );
    }

    [Template(IsPartial = true)]
    public extern void PublicMethod();

    [Template(IsPartial = true)]
    private extern int NonVoidMethod();
}

// <target>
[Introduction]
internal partial class TargetClass { }