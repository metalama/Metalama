#if TEST_OPTIONS
// @TestScenario(DesignTime)
#endif

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.DesignTime.IntroduceEvent;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceEvent( nameof(Event) );
    }

    [Template]
    public event EventHandler? Event
    {
        add { }
        remove { }
    }
}

// <target>
[Introduction]
internal partial class TargetClass { }