using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.Hierarchy;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        var introducedBase = builder.IntroduceInterface( "IIntroducedBase");
        var introducedTest = builder.IntroduceInterface( "ITest");

        introducedTest.ImplementInterface(typeof(TargetType.ISourceBase));
        introducedTest.ImplementInterface(introducedBase.Declaration);
    }
}

// <target>
[IntroductionAttribute]
public class TargetType
{
    public interface ISourceBase;
}