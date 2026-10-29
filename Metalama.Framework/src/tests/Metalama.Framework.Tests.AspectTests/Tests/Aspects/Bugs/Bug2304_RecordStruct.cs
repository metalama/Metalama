// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug2304_RecordStruct;

public sealed class OverrideToStringAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceMethod(
            nameof(ToStringTemplate),
            IntroductionScope.Instance,
            OverrideStrategy.Override,
            m => m.Name = "ToString" );
    }

    [Template]
    public string ToStringTemplate() => "overridden";
}

// <target>
[OverrideToString]
public partial record struct TargetRecord
{
    public string? Code { get; set; }
}

// <target>
public partial record struct TargetRecord
{
    public int Length => Code?.Length ?? 0;
}
