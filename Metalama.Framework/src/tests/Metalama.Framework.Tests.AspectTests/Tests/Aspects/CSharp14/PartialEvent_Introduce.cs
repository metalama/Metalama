// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_5_0_0_OR_GREATER)
#endif

#if ROSLYN_5_0_0_OR_GREATER

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

#pragma warning disable CS0626 // Method, operator, or accessor is marked external and has no attributes on it

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.PartialEvent_Introduce;

public class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceEvent( nameof( this.PartialEvent) );
    }

    [Template( IsPartial = true )]
    public extern event Action PartialEvent;
#pragma warning restore CS0626 // Method, operator, or accessor is marked external and has no attributes on it
}

// <target>
[TheAspect]
internal partial class C
{
}

#endif 