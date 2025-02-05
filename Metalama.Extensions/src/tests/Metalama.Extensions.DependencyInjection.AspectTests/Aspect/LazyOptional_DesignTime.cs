#if TEST_OPTIONS
// @TestScenario(DesignTime)
// @ReportOutputWarnings
#endif

namespace Metalama.Extensions.DependencyInjection.AspectTests.Aspect.LazyOptional_DesignTime;

// <target>
public partial class TargetClass
{
    [Dependency( IsLazy = true, IsRequired = false )]
    private readonly IFormatProvider _formatProvider;

    public TargetClass() { }

    public TargetClass( int x, IFormatProvider existingParameter ) { }
}