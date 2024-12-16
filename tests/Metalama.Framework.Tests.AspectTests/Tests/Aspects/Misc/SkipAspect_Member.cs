using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.SkipAspect_Member;

[assembly: AspectOrder(AspectOrderDirection.RunTime, typeof(IsSkippedAspect), typeof(SkippedAspect))]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.SkipAspect_Member;

public class SkippedAspect : PropertyAspect
{
    public override void BuildAspect(IAspectBuilder<IProperty> builder)
    {
        builder.SkipAspect();
    }

    [Introduce]
    public static bool IsApplied = true;
}

public class IsSkippedAspect : PropertyAspect
{
    [Introduce]
    public static bool IsSkipped
        = meta.Target.Type.Properties.Single().Enhancements().GetAspectInstances().Where(a => a.Aspect is SkippedAspect).Single().IsSkipped;
}

// <target>
public class C
{
    [SkippedAspect]
    [IsSkippedAspect]
    public int A { get; set; }
}