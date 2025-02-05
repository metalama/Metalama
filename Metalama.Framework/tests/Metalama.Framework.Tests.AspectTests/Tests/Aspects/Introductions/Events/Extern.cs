using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

#pragma warning disable CS0626

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Events.Extern;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        builder.IntroduceEvent(nameof(ExternEvent));
    }

    [Template(IsExtern = true)]
    public extern event EventHandler ExternEvent;
}

// <target>
[Introduction]
internal class TargetClass { }