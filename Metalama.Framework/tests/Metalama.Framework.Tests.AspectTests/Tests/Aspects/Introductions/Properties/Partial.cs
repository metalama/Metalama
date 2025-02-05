using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

#pragma warning disable CS0626

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Properties.Partial;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        builder.IntroduceProperty(nameof(PartialProperty));
    }

    [Template(IsPartial = true)]
    public extern int PartialProperty { get; set; }
}

// <target>
[Introduction]
internal partial class TargetClass { }