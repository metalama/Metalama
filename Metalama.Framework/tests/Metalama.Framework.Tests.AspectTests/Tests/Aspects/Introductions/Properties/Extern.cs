using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

#pragma warning disable CS0626

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Properties.Extern;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        builder.IntroduceProperty(nameof(ExternProperty));
    }

    [Template(IsExtern = true)]
    public extern int ExternProperty { get; set; }
}

// <target>
[Introduction]
internal class TargetClass { }