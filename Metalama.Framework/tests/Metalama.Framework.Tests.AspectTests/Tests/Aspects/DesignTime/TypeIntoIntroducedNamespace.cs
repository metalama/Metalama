#if TEST_OPTIONS
// @TestScenario(DesignTime)
#endif

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.DesignTime.TypeIntoIntroducedNamespace;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var ns = builder.With( builder.Target.ContainingNamespace ).WithChildNamespace( "IntroducedNamespace" );
        ns.IntroduceClass( "TestType" );
    }
}

// <target>
[Introduction]
public class TargetType { }