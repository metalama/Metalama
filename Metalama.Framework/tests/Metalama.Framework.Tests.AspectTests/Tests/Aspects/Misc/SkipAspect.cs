using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.SkipAspect;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(IsSkippedAspect), typeof(SkippedAspect) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.SkipAspect;

public class SkippedAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.SkipAspect();
    }

    [Introduce]
    public static bool IsApplied = true;
}

public class IsSkippedAspect : TypeAspect
{
    [Introduce]
    public static bool IsSkipped
        = meta.Target.Type.Enhancements().GetAspectInstances().Where( a => a.Aspect is SkippedAspect ).Single().IsSkipped;
}

// <target>
[SkippedAspect]
[IsSkippedAspect]
public class C { }