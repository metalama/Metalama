// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug582_GetHashCode;

public class OverrideGetHashCodeAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceMethod( nameof(IntroducedGetHashCode), whenExists: OverrideStrategy.Override );
    }

    [Template( Name = "GetHashCode" )]
    public int IntroducedGetHashCode()
    {
        var result = meta.Proceed();

        return result;
    }
}

// <target>
[OverrideGetHashCodeAttribute]
internal record Target;
