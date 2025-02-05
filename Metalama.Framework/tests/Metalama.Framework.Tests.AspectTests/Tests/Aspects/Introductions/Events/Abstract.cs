using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

#pragma warning disable CS0626

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Events.Abstract;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceEvent( nameof(AbstractEvent) );
    }

    [Template]
    public extern event EventHandler AbstractEvent;
}

// <target>
[Introduction]
internal abstract class TargetClass { }