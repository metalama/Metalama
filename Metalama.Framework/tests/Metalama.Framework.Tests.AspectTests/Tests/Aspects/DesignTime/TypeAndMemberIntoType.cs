#if TEST_OPTIONS
// @TestScenario(DesignTime)
#endif

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.DesignTime.TypeAndMemberIntoType;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceAutomaticProperty( "TestProperty", typeof(int) );
        var introducedType = builder.IntroduceClass( "TestType" );
        introducedType.IntroduceAutomaticProperty( "TestProperty", typeof(int) );
    }
}

// <target>
[Introduction]
internal partial class TargetClass { }