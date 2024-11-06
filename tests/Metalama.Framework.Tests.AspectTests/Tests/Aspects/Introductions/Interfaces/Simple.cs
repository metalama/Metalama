using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.Simple;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        builder.IntroduceInterface( "ITest");
        builder.Advice.IntroduceInterface(builder.Target.ContainingNamespace, "ITest2");
    }
}

// <target>
[IntroductionAttribute]
public class TargetType { }