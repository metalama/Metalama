#if TEST_OPTIONS
// @TestScenario(DesignTime)
// @ReportOutputWarnings
#endif

namespace Metalama.Extensions.DependencyInjection.AspectTests.Aspect.EarlyOptional_DesignTime;

// <target>
public partial class TargetClass
{
    [Dependency( IsRequired = false )]
    private readonly IFormatProvider? _formatProvider;

    public TargetClass() { }

    public TargetClass( int x, IFormatProvider existingParameter ) { }
}