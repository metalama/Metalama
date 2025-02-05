using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.IntroducedNestedType;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var type = builder.IntroduceClass( "TestType" );
        type.ImplementInterface( typeof(ITestInterface) );
    }
}

public interface ITestInterface { }

// <target>
[IntroductionAttribute]
public class TargetType { }