#if TEST_OPTIONS
// @TestScenario(DesignTime)
#endif

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.DesignTime.TypeWithInterfaceIntoNonPartialType;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var memento = builder.IntroduceClass( "Memento" );
        memento.ImplementInterface( typeof(IMemento) );
    }
}

internal interface IMemento { }

// <target>
[Introduction]
internal class TargetClass { }