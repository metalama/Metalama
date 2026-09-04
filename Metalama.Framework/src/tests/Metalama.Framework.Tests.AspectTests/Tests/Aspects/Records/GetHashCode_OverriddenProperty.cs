// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Records.GetHashCode_OverriddenProperty;

/// <summary>
/// The aspect overrides both the auto-property and the synthesized <c>GetHashCode</c>. The materialized body is the
/// lowest layer of the member, so it must read the backing field of the property, which is the lowest layer of the
/// property, and not the property itself, which carries the advice.
/// </summary>
public class OverrideAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceMethod( nameof(IntroducedGetHashCode), whenExists: OverrideStrategy.Override );
        builder.With( builder.Target.Properties.Single( p => p.Name == "Value" ) ).Override( nameof(IntroducedProperty) );
    }

    [Template( Name = "GetHashCode" )]
    public int IntroducedGetHashCode()
    {
        return meta.Proceed();
    }

    [Template]
    public dynamic? IntroducedProperty
    {
        get
        {
            return meta.Proceed();
        }

        set
        {
            meta.Proceed();
        }
    }
}

// <target>
[Override]
internal record Target
{
    public int Value { get; set; }
}
