// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System.Linq;
using System.Text;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Records.PrintMembers_OverriddenProperty;

/// <summary>
/// The aspect overrides both the auto-property and the synthesized <c>PrintMembers</c>. The C# compiler calls the
/// getter of the property in <c>PrintMembers</c>, where it reads the backing field in <c>Equals</c>, so the
/// materialized body calls the property and reaches the advice.
/// </summary>
public class OverrideAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceMethod( nameof(IntroducedPrintMembers), whenExists: OverrideStrategy.Override );
        builder.With( builder.Target.Properties.Single( p => p.Name == "Value" ) ).Override( nameof(IntroducedProperty) );
    }

    [Template( Name = "PrintMembers" )]
    protected bool IntroducedPrintMembers( StringBuilder builder )
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
