#if TEST_OPTIONS
// @TestScenario(DesignTime)
#endif

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.DesignTime.TypeIntoType;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceClass( "TestType" );
    }
}

// <target>
[Introduction]
internal partial class TargetClass { }