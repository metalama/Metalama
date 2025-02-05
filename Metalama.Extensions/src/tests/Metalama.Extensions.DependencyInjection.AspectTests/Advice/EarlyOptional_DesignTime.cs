#if TEST_OPTIONS
// @TestScenario(DesignTime)
// @ReportOutputWarnings
#endif

using Metalama.Framework.Aspects;
using Metalama.Extensions.DependencyInjection;
using Metalama.Extensions.DependencyInjection.AspectTests.Advice.EarlyOptional_DesignTime;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(DependencyAttribute), typeof(MyAspect) )]

namespace Metalama.Extensions.DependencyInjection.AspectTests.Advice.EarlyOptional_DesignTime;

public class MyAspect : TypeAspect

{
    [IntroduceDependency( IsRequired = false )]
    private readonly IFormatProvider? _formatProvider;
}

// <target>
[MyAspect]
public partial class TargetClass
{
    public TargetClass() { }

    public TargetClass( int x, IFormatProvider existingParameter ) { }
}