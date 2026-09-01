// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Records.Ineligible_EqualityOperator;

public class OverrideAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceMethod(
            nameof(this.IntroducedEquality),
            IntroductionScope.Static,
            OverrideStrategy.Override,
            buildMethod: m => m.OperatorKind = OperatorKind.Equality );
    }

    [Template]
    public bool IntroducedEquality( Target? left, Target? right )
    {
        return meta.Proceed();
    }
}

// <target>
[Override]
public record Target( int X );
