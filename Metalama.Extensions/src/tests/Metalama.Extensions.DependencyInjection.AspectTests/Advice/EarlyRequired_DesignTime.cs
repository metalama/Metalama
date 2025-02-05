#if TEST_OPTIONS
// @TestScenario(DesignTime)
// @ReportOutputWarnings
#endif

using Metalama.Framework.Aspects;
using Metalama.Extensions.DependencyInjection;
using Metalama.Extensions.DependencyInjection.AspectTests.Advice.EarlyRequired_DesignTime;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(DependencyAttribute), typeof(MyAspect) )]

namespace Metalama.Extensions.DependencyInjection.AspectTests.Advice.EarlyRequired_DesignTime;

// Tests suppression of warnings.

public class MyAspect : TypeAspect
{
    [IntroduceDependency]
    private readonly IFormatProvider _formatProvider;
}

// <target>
[MyAspect]
public partial class TargetClass
{
    public TargetClass() { }

    public TargetClass( int x, IFormatProvider existingParameter ) { }
}